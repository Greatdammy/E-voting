using System.Security.Cryptography;
using EVoting.Application.Common;
using EVoting.Application.DTOs.Elections;
using EVoting.Application.Interfaces;
using EVoting.Domain.Entities;
using EVoting.Domain.Enums;

namespace EVoting.Application.Services;

public class OtpService : IOtpService
{
    private static readonly TimeSpan CodeLifetime = TimeSpan.FromMinutes(5);
    private static readonly TimeSpan ResendCooldown = TimeSpan.FromSeconds(60);
    private static readonly TimeSpan RequestWindow = TimeSpan.FromHours(1);
    private const int MaxRequestsPerWindow = 5;
    private const int MaxVerifyAttempts = 5;

    private readonly IVoteOtpRepository _voteOtpRepository;
    private readonly IOtpCodeHasher _otpCodeHasher;
    private readonly IEmailService _emailService;
    private readonly IUserRepository _userRepository;
    private readonly IElectionRepository _electionRepository;
    private readonly IVoterElectionStatusRepository _voterElectionStatusRepository;
    private readonly IAuditLogService _auditLogService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly TimeProvider _timeProvider;

    public OtpService(
        IVoteOtpRepository voteOtpRepository,
        IOtpCodeHasher otpCodeHasher,
        IEmailService emailService,
        IUserRepository userRepository,
        IElectionRepository electionRepository,
        IVoterElectionStatusRepository voterElectionStatusRepository,
        IAuditLogService auditLogService,
        IUnitOfWork unitOfWork,
        TimeProvider timeProvider)
    {
        _voteOtpRepository = voteOtpRepository;
        _otpCodeHasher = otpCodeHasher;
        _emailService = emailService;
        _userRepository = userRepository;
        _electionRepository = electionRepository;
        _voterElectionStatusRepository = voterElectionStatusRepository;
        _auditLogService = auditLogService;
        _unitOfWork = unitOfWork;
        _timeProvider = timeProvider;
    }

    public async Task<Result<RequestOtpResponseDto>> RequestOtpAsync(Guid userId, Guid electionId)
    {
        var election = await _electionRepository.GetByIdAsync(electionId);
        if (election is null)
        {
            return Result<RequestOtpResponseDto>.Failure(AppError.NotFound, "Election not found.");
        }

        var now = _timeProvider.GetUtcNow().UtcDateTime;
        var status = ElectionStatusCalculator.Compute(election.StartDate, election.EndDate, now);
        if (status != ElectionStatus.Active)
        {
            return Result<RequestOtpResponseDto>.Failure(AppError.ElectionNotActive, "This election is not currently active.");
        }

        var existingStatus = await _voterElectionStatusRepository.GetAsync(userId, electionId);
        if (existingStatus?.HasVoted == true)
        {
            return Result<RequestOtpResponseDto>.Failure(AppError.AlreadyVoted, "You have already voted in this election.");
        }

        var user = await _userRepository.GetByIdAsync(userId);
        if (user is null)
        {
            return Result<RequestOtpResponseDto>.Failure(AppError.NotFound, "Voter account not found.");
        }

        var latest = await _voteOtpRepository.GetLatestAsync(userId, electionId);
        if (latest is not null && now - latest.CreatedAt < ResendCooldown)
        {
            return Result<RequestOtpResponseDto>.Failure(AppError.OtpRequestCooldown, "Please wait before requesting another code.");
        }

        var recentRequestCount = await _voteOtpRepository.CountRequestsSinceAsync(userId, electionId, now - RequestWindow);
        if (recentRequestCount >= MaxRequestsPerWindow)
        {
            return Result<RequestOtpResponseDto>.Failure(AppError.OtpRequestLimitExceeded, "Too many verification codes requested. Try again later.");
        }

        // Only the most recently issued code is ever valid - supersede whatever
        // is still outstanding so a leaked/old code can't be replayed.
        if (latest is not null && !latest.IsUsed)
        {
            latest.IsUsed = true;
        }

        var code = GenerateCode();
        var otp = new VoteOtp
        {
            UserId = userId,
            ElectionId = electionId,
            CodeHash = _otpCodeHasher.Hash(code),
            ExpiresAt = now.Add(CodeLifetime),
            CreatedAt = now
        };

        await _voteOtpRepository.AddAsync(otp);
        await _auditLogService.LogAsync(userId, $"OTP requested for election {electionId}");
        await _unitOfWork.SaveChangesAsync();

        await _emailService.SendOtpEmailAsync(user.Email, code, CodeLifetime);

        return Result<RequestOtpResponseDto>.Success(new RequestOtpResponseDto
        {
            ExpiresAt = otp.ExpiresAt,
            MaskedEmail = MaskEmail(user.Email)
        });
    }

    public async Task<Result<bool>> VerifyAndConsumeAsync(Guid userId, Guid electionId, string code)
    {
        var otp = await _voteOtpRepository.GetLatestAsync(userId, electionId);
        var now = _timeProvider.GetUtcNow().UtcDateTime;

        if (otp is null || otp.IsUsed)
        {
            return Result<bool>.Failure(AppError.OtpNotFound, "Request a verification code before voting.");
        }

        if (otp.ExpiresAt <= now)
        {
            return Result<bool>.Failure(AppError.OtpExpired, "This verification code has expired. Request a new one.");
        }

        if (otp.AttemptCount >= MaxVerifyAttempts)
        {
            return Result<bool>.Failure(AppError.OtpAttemptsExceeded, "Too many incorrect attempts. Request a new code.");
        }

        if (!_otpCodeHasher.Verify(code, otp.CodeHash))
        {
            // Persisted immediately, independent of whatever the caller does
            // next - a wrong guess must count even if the vote itself never
            // reaches a transaction.
            otp.AttemptCount++;
            await _auditLogService.LogAsync(userId, $"OTP verification failed for election {electionId}");
            await _unitOfWork.SaveChangesAsync();
            return Result<bool>.Failure(AppError.OtpInvalid, "Incorrect verification code.");
        }

        // Left unsaved deliberately: the caller (VoteService) persists this
        // alongside the vote insert in one transaction, so a code is only
        // ever "spent" together with a vote that actually took effect.
        otp.IsUsed = true;
        await _auditLogService.LogAsync(userId, $"OTP verified for election {electionId}");

        return Result<bool>.Success(true);
    }

    private static string GenerateCode()
    {
        var value = RandomNumberGenerator.GetInt32(0, 1_000_000);
        return value.ToString("D6");
    }

    private static string MaskEmail(string email)
    {
        var atIndex = email.IndexOf('@');
        if (atIndex <= 1)
        {
            return email;
        }

        return $"{email[..2]}***{email[atIndex..]}";
    }
}