using EVoting.Application.Common;
using EVoting.Application.DTOs.Admin;
using EVoting.Application.DTOs.Auth;

namespace EVoting.Application.Interfaces;

public interface IAuthService
{
    Task<Result<RegisterResponseDto>> RegisterAsync(RegisterRequestDto request);
    Task<Result<LoginResponseDto>> LoginAsync(LoginRequestDto request);
    Task<Result<CreateUserResponseDto>> CreateUserAsync(CreateUserRequestDto request, Guid createdByUserId);
}
