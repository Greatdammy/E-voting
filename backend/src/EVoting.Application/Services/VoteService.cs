using EVoting.Application.Common;
using EVoting.Application.DTOs.Elections;
using EVoting.Application.Interfaces;
using EVoting.Domain.Entities;
using EVoting.Domain.Enums;

namespace EVoting.Application.Services;

public class VoteService : IVoteService
{
    private readonly IElectionRepository _electionRepository;
    private readonly ICandidateRepository _candidateRepository;
    private readonly IVoteRepository _voteRepository;
    private readonly IVoterElectionStatusRepository _voterElectionStatusRepository;
    private readonly IVoterAnonymizer _voterAnonymizer;
    private readonly IConfirmationHashService _confirmationHashService;
    private readonly IResultsBroadcaster _resultsBroadcaster;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IOtpService _otpService;

    public VoteService(
        IElectionRepository electionRepository,
        ICandidateRepository candidateRepository,
        IVoteRepository voteRepository,
        IVoterElectionStatusRepository voterElectionStatusRepository,
        IVoterAnonymizer voterAnonymizer,
        IConfirmationHashService confirmationHashService,
        IResultsBroadcaster resultsBroadcaster,
        IUnitOfWork unitOfWork,
        IOtpService otpService)
    {
        _electionRepository = electionRepository;
        _candidateRepository = candidateRepository;
        _voteRepository = voteRepository;
        _voterElectionStatusRepository = voterElectionStatusRepository;
        _voterAnonymizer = voterAnonymizer;
        _confirmationHashService = confirmationHashService;
        _resultsBroadcaster = resultsBroadcaster;
        _unitOfWork = unitOfWork;
        _otpService = otpService;
    }

    public async Task<Result<CastVoteResponseDto>> CastVoteAsync(Guid electionId, Guid voterId, CastVoteRequestDto request)
    {
        var election = await _electionRepository.GetByIdAsync(electionId);
        if (election is null)
        {
            return Result<CastVoteResponseDto>.Failure(AppError.NotFound, "Election not found.");
        }

        var status = ElectionStatusCalculator.Compute(election.StartDate, election.EndDate, DateTime.UtcNow);
        if (status != ElectionStatus.Active)
        {
            return Result<CastVoteResponseDto>.Failure(AppError.ElectionNotActive, "This election is not currently active.");
        }

        var candidate = await _candidateRepository.GetByIdAsync(request.CandidateId);
        if (candidate is null || candidate.ElectionId != electionId)
        {
            return Result<CastVoteResponseDto>.Failure(AppError.InvalidCandidate, "Candidate does not belong to this election.");
        }

        var existingStatus = await _voterElectionStatusRepository.GetAsync(voterId, electionId);
        if (existingStatus?.HasVoted == true)
        {
            return Result<CastVoteResponseDto>.Failure(AppError.AlreadyVoted, "You have already voted in this election.");
        }

        // Checked last, right before the transaction: it's the only check
        // that mutates state (attempt count / consumption), so cheap
        // read-only checks above should fail first and leave the code alone.
        var otpVerification = await _otpService.VerifyAndConsumeAsync(voterId, electionId, request.OtpCode);
        if (!otpVerification.Succeeded)
        {
            return Result<CastVoteResponseDto>.Failure(otpVerification.Error, otpVerification.ErrorMessage!);
        }

        if (election.Status != status)
        {
            election.Status = status;
        }

        await _unitOfWork.BeginTransactionAsync();

        try
        {
            var vote = new Vote
            {
                ElectionId = electionId,
                VoterId = _voterAnonymizer.ComputeVoterId(voterId),
                CandidateId = request.CandidateId,
                VotedAt = DateTime.UtcNow
            };

            await _voteRepository.AddAsync(vote);

            if (existingStatus is null)
            {
                await _voterElectionStatusRepository.AddAsync(new VoterElectionStatus
                {
                    UserId = voterId,
                    ElectionId = electionId,
                    HasVoted = true,
                    VotedAt = vote.VotedAt
                });
            }
            else
            {
                existingStatus.HasVoted = true;
                existingStatus.VotedAt = vote.VotedAt;
            }

            vote.VoteHash = _confirmationHashService.Compute(vote.VoteId, electionId);

            await _unitOfWork.SaveChangesAsync();
            await _unitOfWork.CommitAsync();

            var tally = await _voteRepository.GetTallyAsync(electionId);
            await _resultsBroadcaster.BroadcastResultsAsync(electionId, new ResultsResponseDto
            {
                ElectionId = electionId,
                Title = election.Title,
                Status = status.ToString(),
                Tally = tally,
                TotalVotes = tally.Sum(t => t.VoteCount)
            });

            return Result<CastVoteResponseDto>.Success(new CastVoteResponseDto
            {
                VoteId = vote.VoteId,
                ConfirmationHash = vote.VoteHash,
                VotedAt = vote.VotedAt
            });
        }
        catch (UniqueConstraintViolationException)
        {
            await _unitOfWork.RollbackAsync();
            return Result<CastVoteResponseDto>.Failure(AppError.AlreadyVoted, "You have already voted in this election.");
        }
    }
}
