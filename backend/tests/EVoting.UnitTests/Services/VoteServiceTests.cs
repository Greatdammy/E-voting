using EVoting.Application.Common;
using EVoting.Application.DTOs.Elections;
using EVoting.Application.Interfaces;
using EVoting.Application.Services;
using EVoting.Domain.Entities;
using EVoting.Domain.Enums;
using Moq;
using Xunit;

namespace EVoting.UnitTests.Services;

public class VoteServiceTests
{
    private readonly Mock<IElectionRepository> _electionRepository = new();
    private readonly Mock<ICandidateRepository> _candidateRepository = new();
    private readonly Mock<IVoteRepository> _voteRepository = new();
    private readonly Mock<IVoterElectionStatusRepository> _voterElectionStatusRepository = new();
    private readonly Mock<IVoterAnonymizer> _voterAnonymizer = new();
    private readonly Mock<IConfirmationHashService> _confirmationHashService = new();
    private readonly Mock<IResultsBroadcaster> _resultsBroadcaster = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly Mock<IOtpService> _otpService = new();
    private readonly VoteService _sut;

    public VoteServiceTests()
    {
        _sut = new VoteService(
            _electionRepository.Object,
            _candidateRepository.Object,
            _voteRepository.Object,
            _voterElectionStatusRepository.Object,
            _voterAnonymizer.Object,
            _confirmationHashService.Object,
            _resultsBroadcaster.Object,
            _unitOfWork.Object,
            _otpService.Object);

        // Valid by default so existing-behavior tests don't all need to
        // repeat this setup; individual tests override it to exercise the
        // OTP-rejection paths.
        _otpService
            .Setup(o => o.VerifyAndConsumeAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<string>()))
            .ReturnsAsync(Result<bool>.Success(true));
    }

    private static Election ActiveElection() => new()
    {
        StartDate = DateTime.UtcNow.AddHours(-1),
        EndDate = DateTime.UtcNow.AddHours(1)
    };

    private static CastVoteRequestDto Request(Guid candidateId) => new() { CandidateId = candidateId, OtpCode = "123456" };

    [Fact]
    public async Task CastVoteAsync_Succeeds_WhenElectionActiveAndVoterHasNotVoted()
    {
        var election = ActiveElection();
        var candidate = new Candidate { ElectionId = election.ElectionId };
        var voterId = Guid.NewGuid();

        _electionRepository.Setup(r => r.GetByIdAsync(election.ElectionId)).ReturnsAsync(election);
        _candidateRepository.Setup(r => r.GetByIdAsync(candidate.CandidateId)).ReturnsAsync(candidate);
        _voterElectionStatusRepository.Setup(r => r.GetAsync(voterId, election.ElectionId)).ReturnsAsync((VoterElectionStatus?)null);
        _voterAnonymizer.Setup(a => a.ComputeVoterId(voterId)).Returns("hashed-voter-id");
        _confirmationHashService.Setup(c => c.Compute(It.IsAny<Guid>(), election.ElectionId)).Returns("confirmation-hash");
        _voteRepository.Setup(r => r.GetTallyAsync(election.ElectionId)).ReturnsAsync(new List<CandidateTallyDto>());

        var request = Request(candidate.CandidateId);

        var result = await _sut.CastVoteAsync(election.ElectionId, voterId, request);

        Assert.True(result.Succeeded);
        Assert.Equal("confirmation-hash", result.Value!.ConfirmationHash);
        _voteRepository.Verify(
            r => r.AddAsync(It.Is<Vote>(v => v.VoterId == "hashed-voter-id" && v.CandidateId == candidate.CandidateId)),
            Times.Once);
        _voterElectionStatusRepository.Verify(
            r => r.AddAsync(It.Is<VoterElectionStatus>(s => s.UserId == voterId && s.HasVoted)),
            Times.Once);
        _otpService.Verify(o => o.VerifyAndConsumeAsync(voterId, election.ElectionId, "123456"), Times.Once);
        _unitOfWork.Verify(u => u.BeginTransactionAsync(), Times.Once);
        _unitOfWork.Verify(u => u.CommitAsync(), Times.Once);
        _resultsBroadcaster.Verify(b => b.BroadcastResultsAsync(election.ElectionId, It.IsAny<ResultsResponseDto>()), Times.Once);
    }

    [Fact]
    public async Task CastVoteAsync_ReturnsNotFound_WhenElectionDoesNotExist()
    {
        var electionId = Guid.NewGuid();
        _electionRepository.Setup(r => r.GetByIdAsync(electionId)).ReturnsAsync((Election?)null);

        var result = await _sut.CastVoteAsync(electionId, Guid.NewGuid(), Request(Guid.NewGuid()));

        Assert.False(result.Succeeded);
        Assert.Equal(AppError.NotFound, result.Error);
    }

    [Fact]
    public async Task CastVoteAsync_ReturnsElectionNotActive_WhenElectionIsUpcoming()
    {
        var election = new Election { StartDate = DateTime.UtcNow.AddDays(1), EndDate = DateTime.UtcNow.AddDays(2) };
        _electionRepository.Setup(r => r.GetByIdAsync(election.ElectionId)).ReturnsAsync(election);

        var result = await _sut.CastVoteAsync(election.ElectionId, Guid.NewGuid(), Request(Guid.NewGuid()));

        Assert.False(result.Succeeded);
        Assert.Equal(AppError.ElectionNotActive, result.Error);
    }

    [Fact]
    public async Task CastVoteAsync_ReturnsInvalidCandidate_WhenCandidateBelongsToDifferentElection()
    {
        var election = ActiveElection();
        var candidate = new Candidate { ElectionId = Guid.NewGuid() };
        _electionRepository.Setup(r => r.GetByIdAsync(election.ElectionId)).ReturnsAsync(election);
        _candidateRepository.Setup(r => r.GetByIdAsync(candidate.CandidateId)).ReturnsAsync(candidate);

        var result = await _sut.CastVoteAsync(election.ElectionId, Guid.NewGuid(), Request(candidate.CandidateId));

        Assert.False(result.Succeeded);
        Assert.Equal(AppError.InvalidCandidate, result.Error);
    }

    [Fact]
    public async Task CastVoteAsync_ReturnsAlreadyVoted_WhenVoterElectionStatusHasVoted()
    {
        var election = ActiveElection();
        var candidate = new Candidate { ElectionId = election.ElectionId };
        var voterId = Guid.NewGuid();

        _electionRepository.Setup(r => r.GetByIdAsync(election.ElectionId)).ReturnsAsync(election);
        _candidateRepository.Setup(r => r.GetByIdAsync(candidate.CandidateId)).ReturnsAsync(candidate);
        _voterElectionStatusRepository
            .Setup(r => r.GetAsync(voterId, election.ElectionId))
            .ReturnsAsync(new VoterElectionStatus { UserId = voterId, ElectionId = election.ElectionId, HasVoted = true });

        var result = await _sut.CastVoteAsync(election.ElectionId, voterId, Request(candidate.CandidateId));

        Assert.False(result.Succeeded);
        Assert.Equal(AppError.AlreadyVoted, result.Error);
        _voteRepository.Verify(r => r.AddAsync(It.IsAny<Vote>()), Times.Never);
    }

    [Fact]
    public async Task CastVoteAsync_ReturnsAlreadyVoted_WhenSaveChangesThrowsUniqueConstraintViolation()
    {
        var election = ActiveElection();
        var candidate = new Candidate { ElectionId = election.ElectionId };
        var voterId = Guid.NewGuid();

        _electionRepository.Setup(r => r.GetByIdAsync(election.ElectionId)).ReturnsAsync(election);
        _candidateRepository.Setup(r => r.GetByIdAsync(candidate.CandidateId)).ReturnsAsync(candidate);
        _voterElectionStatusRepository.Setup(r => r.GetAsync(voterId, election.ElectionId)).ReturnsAsync((VoterElectionStatus?)null);
        _voterAnonymizer.Setup(a => a.ComputeVoterId(voterId)).Returns("hashed-voter-id");
        _unitOfWork.Setup(u => u.SaveChangesAsync()).ThrowsAsync(new UniqueConstraintViolationException());

        var result = await _sut.CastVoteAsync(election.ElectionId, voterId, Request(candidate.CandidateId));

        Assert.False(result.Succeeded);
        Assert.Equal(AppError.AlreadyVoted, result.Error);
        _unitOfWork.Verify(u => u.RollbackAsync(), Times.Once);
        _resultsBroadcaster.Verify(b => b.BroadcastResultsAsync(It.IsAny<Guid>(), It.IsAny<ResultsResponseDto>()), Times.Never);
    }

    [Fact]
    public async Task CastVoteAsync_ReturnsOtpError_WhenOtpVerificationFails_AndDoesNotOpenATransaction()
    {
        var election = ActiveElection();
        var candidate = new Candidate { ElectionId = election.ElectionId };
        var voterId = Guid.NewGuid();

        _electionRepository.Setup(r => r.GetByIdAsync(election.ElectionId)).ReturnsAsync(election);
        _candidateRepository.Setup(r => r.GetByIdAsync(candidate.CandidateId)).ReturnsAsync(candidate);
        _voterElectionStatusRepository.Setup(r => r.GetAsync(voterId, election.ElectionId)).ReturnsAsync((VoterElectionStatus?)null);
        _otpService
            .Setup(o => o.VerifyAndConsumeAsync(voterId, election.ElectionId, "000000"))
            .ReturnsAsync(Result<bool>.Failure(AppError.OtpInvalid, "Incorrect verification code."));

        var result = await _sut.CastVoteAsync(election.ElectionId, voterId, new CastVoteRequestDto { CandidateId = candidate.CandidateId, OtpCode = "000000" });

        Assert.False(result.Succeeded);
        Assert.Equal(AppError.OtpInvalid, result.Error);
        _voteRepository.Verify(r => r.AddAsync(It.IsAny<Vote>()), Times.Never);
        _unitOfWork.Verify(u => u.BeginTransactionAsync(), Times.Never);
    }
}