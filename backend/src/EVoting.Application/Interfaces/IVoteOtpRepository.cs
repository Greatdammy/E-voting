using EVoting.Domain.Entities;

namespace EVoting.Application.Interfaces;

public interface IVoteOtpRepository
{
    Task<VoteOtp?> GetLatestAsync(Guid userId, Guid electionId);
    Task<int> CountRequestsSinceAsync(Guid userId, Guid electionId, DateTime sinceUtc);
    Task AddAsync(VoteOtp otp);
}