using EVoting.Application.Common;
using EVoting.Application.DTOs.Elections;

namespace EVoting.Application.Interfaces;

public interface IOtpService
{
    Task<Result<RequestOtpResponseDto>> RequestOtpAsync(Guid userId, Guid electionId);
    Task<Result<bool>> VerifyAndConsumeAsync(Guid userId, Guid electionId, string code);
}