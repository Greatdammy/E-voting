using EVoting.Application.Common;
using EVoting.Application.DTOs.Elections;
using EVoting.Application.Interfaces;
using EVoting.Domain.Entities;
using EVoting.Domain.Enums;

namespace EVoting.Application.Services;

public class ElectionService : IElectionService
{
    private readonly IElectionRepository _electionRepository;
    private readonly ICandidateRepository _candidateRepository;
    private readonly IVoterElectionStatusRepository _voterElectionStatusRepository;
    private readonly IVoteRepository _voteRepository;
    private readonly IUnitOfWork _unitOfWork;

    public ElectionService(
        IElectionRepository electionRepository,
        ICandidateRepository candidateRepository,
        IVoterElectionStatusRepository voterElectionStatusRepository,
        IVoteRepository voteRepository,
        IUnitOfWork unitOfWork)
    {
        _electionRepository = electionRepository;
        _candidateRepository = candidateRepository;
        _voterElectionStatusRepository = voterElectionStatusRepository;
        _voteRepository = voteRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<ElectionResponseDto>> CreateElectionAsync(CreateElectionRequestDto request, Guid createdBy)
    {
        var election = new Election
        {
            Title = request.Title,
            Description = request.Description,
            StartDate = request.StartDate,
            EndDate = request.EndDate,
            CreatedBy = createdBy
        };
        election.Status = ElectionStatusCalculator.Compute(election.StartDate, election.EndDate, DateTime.UtcNow);

        await _electionRepository.AddAsync(election);
        await _unitOfWork.SaveChangesAsync();

        return Result<ElectionResponseDto>.Success(ToElectionResponseDto(election));
    }

    public async Task<Result<List<ElectionSummaryDto>>> GetElectionsForVoterAsync(Guid voterId)
    {
        var elections = await _electionRepository.ListAsync();
        var votedElectionIds = await _voterElectionStatusRepository.GetVotedElectionIdsAsync(voterId);
        var now = DateTime.UtcNow;

        var summaries = elections.Select(election => new ElectionSummaryDto
        {
            ElectionId = election.ElectionId,
            Title = election.Title,
            Description = election.Description,
            StartDate = election.StartDate,
            EndDate = election.EndDate,
            Status = ElectionStatusCalculator.Compute(election.StartDate, election.EndDate, now).ToString(),
            HasVoted = votedElectionIds.Contains(election.ElectionId)
        }).ToList();

        return Result<List<ElectionSummaryDto>>.Success(summaries);
    }

    public async Task<Result<BallotResponseDto>> GetBallotAsync(Guid electionId, Guid voterId)
    {
        var election = await _electionRepository.GetByIdAsync(electionId);
        if (election is null)
        {
            return Result<BallotResponseDto>.Failure(AppError.NotFound, "Election not found.");
        }

        var status = ElectionStatusCalculator.Compute(election.StartDate, election.EndDate, DateTime.UtcNow);
        if (status != ElectionStatus.Active)
        {
            return Result<BallotResponseDto>.Failure(AppError.ElectionNotActive, "This election is not currently active.");
        }

        var voterStatus = await _voterElectionStatusRepository.GetAsync(voterId, electionId);
        if (voterStatus?.HasVoted == true)
        {
            return Result<BallotResponseDto>.Failure(AppError.AlreadyVoted, "You have already voted in this election.");
        }

        var candidates = await _candidateRepository.ListByElectionAsync(electionId);

        return Result<BallotResponseDto>.Success(new BallotResponseDto
        {
            ElectionId = election.ElectionId,
            Title = election.Title,
            Description = election.Description,
            Candidates = candidates.Select(c => new BallotCandidateDto
            {
                CandidateId = c.CandidateId,
                Name = c.Name,
                Party = c.Party,
                PhotoUrl = c.PhotoUrl
            }).ToList()
        });
    }

    public async Task<Result<ResultsResponseDto>> GetResultsAsync(Guid electionId)
    {
        var election = await _electionRepository.GetByIdAsync(electionId);
        if (election is null)
        {
            return Result<ResultsResponseDto>.Failure(AppError.NotFound, "Election not found.");
        }

        var status = ElectionStatusCalculator.Compute(election.StartDate, election.EndDate, DateTime.UtcNow);
        var tally = await _voteRepository.GetTallyAsync(electionId);

        return Result<ResultsResponseDto>.Success(new ResultsResponseDto
        {
            ElectionId = election.ElectionId,
            Title = election.Title,
            Status = status.ToString(),
            Tally = tally,
            TotalVotes = tally.Sum(t => t.VoteCount)
        });
    }

    private static ElectionResponseDto ToElectionResponseDto(Election election) => new()
    {
        ElectionId = election.ElectionId,
        Title = election.Title,
        Description = election.Description,
        StartDate = election.StartDate,
        EndDate = election.EndDate,
        Status = election.Status.ToString(),
        CreatedBy = election.CreatedBy
    };
}
