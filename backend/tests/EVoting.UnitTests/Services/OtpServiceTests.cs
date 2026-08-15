using EVoting.Application.Common;
using EVoting.Application.Interfaces;
using EVoting.Application.Services;
using EVoting.Domain.Entities;
using EVoting.Domain.Enums;
using Moq;
using Xunit;

namespace EVoting.UnitTests.Services;

public class OtpServiceTests
{
    private static readonly DateTimeOffset Now = new(2026, 1, 1, 12, 0, 0, TimeSpan.Zero);

    private readonly Mock<IVoteOtpRepository> _voteOtpRepository = new();
    private readonly Mock<IOtpCodeHasher> _otpCodeHasher = new();
    private readonly Mock<IEmailService> _emailService = new();
    private readonly Mock<IUserRepository> _userRepository = new();
    private readonly Mock<IElectionRepository> _electionRepository = new();
    private readonly Mock<IVoterElectionStatusRepository> _voterElectionStatusRepository = new();
    private readonly Mock<IAuditLogService> _auditLogService = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly OtpService _sut;

    public OtpServiceTests()
    {
        _sut = new OtpService(
            _voteOtpRepository.Object,
            _otpCodeHasher.Object,
            _emailService.Object,
            _userRepository.Object,
            _electionRepository.Object,
            _voterElectionStatusRepository.Object,
            _auditLogService.Object,
            _unitOfWork.Object,
            new FixedTimeProvider(Now));
    }

    private static Election ActiveElection() => new()
    {
        StartDate = Now.UtcDateTime.AddHours(-1),
        EndDate = Now.UtcDateTime.AddHours(1)
    };

    [Fact]
    public async Task RequestOtpAsync_Succeeds_SendsEmail_AndReturnsMaskedAddress()
    {
        var election = ActiveElection();
        var userId = Guid.NewGuid();
        var user = new User { UserId = userId, Email = "voter@example.com" };

        _electionRepository.Setup(r => r.GetByIdAsync(election.ElectionId)).ReturnsAsync(election);
        _voterElectionStatusRepository.Setup(r => r.GetAsync(userId, election.ElectionId)).ReturnsAsync((VoterElectionStatus?)null);
        _userRepository.Setup(r => r.GetByIdAsync(userId)).ReturnsAsync(user);
        _voteOtpRepository.Setup(r => r.GetLatestAsync(userId, election.ElectionId)).ReturnsAsync((VoteOtp?)null);
        _voteOtpRepository.Setup(r => r.CountRequestsSinceAsync(userId, election.ElectionId, It.IsAny<DateTime>())).ReturnsAsync(0);
        _otpCodeHasher.Setup(h => h.Hash(It.IsAny<string>())).Returns("hashed-code");

        var result = await _sut.RequestOtpAsync(userId, election.ElectionId);

        Assert.True(result.Succeeded);
        Assert.Equal("vo***@example.com", result.Value!.MaskedEmail);
        Assert.Equal(Now.UtcDateTime.AddMinutes(5), result.Value.ExpiresAt);
        _emailService.Verify(e => e.SendOtpEmailAsync("voter@example.com", It.IsAny<string>(), It.IsAny<TimeSpan>()), Times.Once);
        _voteOtpRepository.Verify(r => r.AddAsync(It.Is<VoteOtp>(o => o.UserId == userId && o.ElectionId == election.ElectionId)), Times.Once);
        _unitOfWork.Verify(u => u.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task RequestOtpAsync_ReturnsElectionNotActive_WhenElectionIsUpcoming()
    {
        var election = new Election { StartDate = Now.UtcDateTime.AddDays(1), EndDate = Now.UtcDateTime.AddDays(2) };
        _electionRepository.Setup(r => r.GetByIdAsync(election.ElectionId)).ReturnsAsync(election);

        var result = await _sut.RequestOtpAsync(Guid.NewGuid(), election.ElectionId);

        Assert.False(result.Succeeded);
        Assert.Equal(AppError.ElectionNotActive, result.Error);
        _emailService.Verify(e => e.SendOtpEmailAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<TimeSpan>()), Times.Never);
    }

    [Fact]
    public async Task RequestOtpAsync_ReturnsAlreadyVoted_WhenVoterHasVoted()
    {
        var election = ActiveElection();
        var userId = Guid.NewGuid();
        _electionRepository.Setup(r => r.GetByIdAsync(election.ElectionId)).ReturnsAsync(election);
        _voterElectionStatusRepository
            .Setup(r => r.GetAsync(userId, election.ElectionId))
            .ReturnsAsync(new VoterElectionStatus { UserId = userId, ElectionId = election.ElectionId, HasVoted = true });

        var result = await _sut.RequestOtpAsync(userId, election.ElectionId);

        Assert.False(result.Succeeded);
        Assert.Equal(AppError.AlreadyVoted, result.Error);
    }

    [Fact]
    public async Task RequestOtpAsync_ReturnsCooldown_WhenRequestedWithin60Seconds()
    {
        var election = ActiveElection();
        var userId = Guid.NewGuid();
        var user = new User { UserId = userId, Email = "voter@example.com" };

        _electionRepository.Setup(r => r.GetByIdAsync(election.ElectionId)).ReturnsAsync(election);
        _voterElectionStatusRepository.Setup(r => r.GetAsync(userId, election.ElectionId)).ReturnsAsync((VoterElectionStatus?)null);
        _userRepository.Setup(r => r.GetByIdAsync(userId)).ReturnsAsync(user);
        _voteOtpRepository
            .Setup(r => r.GetLatestAsync(userId, election.ElectionId))
            .ReturnsAsync(new VoteOtp { UserId = userId, ElectionId = election.ElectionId, CreatedAt = Now.UtcDateTime.AddSeconds(-30) });

        var result = await _sut.RequestOtpAsync(userId, election.ElectionId);

        Assert.False(result.Succeeded);
        Assert.Equal(AppError.OtpRequestCooldown, result.Error);
        _emailService.Verify(e => e.SendOtpEmailAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<TimeSpan>()), Times.Never);
    }

    [Fact]
    public async Task RequestOtpAsync_ReturnsLimitExceeded_WhenFiveRequestsAlreadyMadeThisHour()
    {
        var election = ActiveElection();
        var userId = Guid.NewGuid();
        var user = new User { UserId = userId, Email = "voter@example.com" };

        _electionRepository.Setup(r => r.GetByIdAsync(election.ElectionId)).ReturnsAsync(election);
        _voterElectionStatusRepository.Setup(r => r.GetAsync(userId, election.ElectionId)).ReturnsAsync((VoterElectionStatus?)null);
        _userRepository.Setup(r => r.GetByIdAsync(userId)).ReturnsAsync(user);
        _voteOtpRepository
            .Setup(r => r.GetLatestAsync(userId, election.ElectionId))
            .ReturnsAsync(new VoteOtp { UserId = userId, ElectionId = election.ElectionId, CreatedAt = Now.UtcDateTime.AddMinutes(-10) });
        _voteOtpRepository.Setup(r => r.CountRequestsSinceAsync(userId, election.ElectionId, It.IsAny<DateTime>())).ReturnsAsync(5);

        var result = await _sut.RequestOtpAsync(userId, election.ElectionId);

        Assert.False(result.Succeeded);
        Assert.Equal(AppError.OtpRequestLimitExceeded, result.Error);
    }

    [Fact]
    public async Task RequestOtpAsync_SupersedesThePreviousUnusedCode()
    {
        var election = ActiveElection();
        var userId = Guid.NewGuid();
        var user = new User { UserId = userId, Email = "voter@example.com" };
        var previous = new VoteOtp { UserId = userId, ElectionId = election.ElectionId, CreatedAt = Now.UtcDateTime.AddMinutes(-10), IsUsed = false };

        _electionRepository.Setup(r => r.GetByIdAsync(election.ElectionId)).ReturnsAsync(election);
        _voterElectionStatusRepository.Setup(r => r.GetAsync(userId, election.ElectionId)).ReturnsAsync((VoterElectionStatus?)null);
        _userRepository.Setup(r => r.GetByIdAsync(userId)).ReturnsAsync(user);
        _voteOtpRepository.Setup(r => r.GetLatestAsync(userId, election.ElectionId)).ReturnsAsync(previous);
        _voteOtpRepository.Setup(r => r.CountRequestsSinceAsync(userId, election.ElectionId, It.IsAny<DateTime>())).ReturnsAsync(1);

        var result = await _sut.RequestOtpAsync(userId, election.ElectionId);

        Assert.True(result.Succeeded);
        Assert.True(previous.IsUsed);
    }

    [Fact]
    public async Task VerifyAndConsumeAsync_ReturnsNotFound_WhenNoOtpExists()
    {
        var userId = Guid.NewGuid();
        var electionId = Guid.NewGuid();
        _voteOtpRepository.Setup(r => r.GetLatestAsync(userId, electionId)).ReturnsAsync((VoteOtp?)null);

        var result = await _sut.VerifyAndConsumeAsync(userId, electionId, "123456");

        Assert.False(result.Succeeded);
        Assert.Equal(AppError.OtpNotFound, result.Error);
    }

    [Fact]
    public async Task VerifyAndConsumeAsync_ReturnsExpired_WhenPastExpiry()
    {
        var userId = Guid.NewGuid();
        var electionId = Guid.NewGuid();
        var otp = new VoteOtp { UserId = userId, ElectionId = electionId, ExpiresAt = Now.UtcDateTime.AddMinutes(-1), CodeHash = "hash" };
        _voteOtpRepository.Setup(r => r.GetLatestAsync(userId, electionId)).ReturnsAsync(otp);

        var result = await _sut.VerifyAndConsumeAsync(userId, electionId, "123456");

        Assert.False(result.Succeeded);
        Assert.Equal(AppError.OtpExpired, result.Error);
    }

    [Fact]
    public async Task VerifyAndConsumeAsync_ReturnsAttemptsExceeded_WhenAtMaxAttempts()
    {
        var userId = Guid.NewGuid();
        var electionId = Guid.NewGuid();
        var otp = new VoteOtp
        {
            UserId = userId,
            ElectionId = electionId,
            ExpiresAt = Now.UtcDateTime.AddMinutes(1),
            CodeHash = "hash",
            AttemptCount = 5
        };
        _voteOtpRepository.Setup(r => r.GetLatestAsync(userId, electionId)).ReturnsAsync(otp);

        var result = await _sut.VerifyAndConsumeAsync(userId, electionId, "123456");

        Assert.False(result.Succeeded);
        Assert.Equal(AppError.OtpAttemptsExceeded, result.Error);
    }

    [Fact]
    public async Task VerifyAndConsumeAsync_IncrementsAttemptCount_AndSavesImmediately_WhenCodeDoesNotMatch()
    {
        var userId = Guid.NewGuid();
        var electionId = Guid.NewGuid();
        var otp = new VoteOtp
        {
            UserId = userId,
            ElectionId = electionId,
            ExpiresAt = Now.UtcDateTime.AddMinutes(1),
            CodeHash = "hash",
            AttemptCount = 0
        };
        _voteOtpRepository.Setup(r => r.GetLatestAsync(userId, electionId)).ReturnsAsync(otp);
        _otpCodeHasher.Setup(h => h.Verify("000000", "hash")).Returns(false);

        var result = await _sut.VerifyAndConsumeAsync(userId, electionId, "000000");

        Assert.False(result.Succeeded);
        Assert.Equal(AppError.OtpInvalid, result.Error);
        Assert.Equal(1, otp.AttemptCount);
        Assert.False(otp.IsUsed);
        _unitOfWork.Verify(u => u.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task VerifyAndConsumeAsync_MarksUsed_ButDoesNotSave_WhenCodeMatches()
    {
        var userId = Guid.NewGuid();
        var electionId = Guid.NewGuid();
        var otp = new VoteOtp
        {
            UserId = userId,
            ElectionId = electionId,
            ExpiresAt = Now.UtcDateTime.AddMinutes(1),
            CodeHash = "hash",
            AttemptCount = 0
        };
        _voteOtpRepository.Setup(r => r.GetLatestAsync(userId, electionId)).ReturnsAsync(otp);
        _otpCodeHasher.Setup(h => h.Verify("123456", "hash")).Returns(true);

        var result = await _sut.VerifyAndConsumeAsync(userId, electionId, "123456");

        Assert.True(result.Succeeded);
        Assert.True(otp.IsUsed);
        // Deliberately left for the caller (VoteService) to persist alongside
        // the vote insert, in the same transaction.
        _unitOfWork.Verify(u => u.SaveChangesAsync(), Times.Never);
    }

    private sealed class FixedTimeProvider : TimeProvider
    {
        private readonly DateTimeOffset _now;

        public FixedTimeProvider(DateTimeOffset now)
        {
            _now = now;
        }

        public override DateTimeOffset GetUtcNow() => _now;
    }
}