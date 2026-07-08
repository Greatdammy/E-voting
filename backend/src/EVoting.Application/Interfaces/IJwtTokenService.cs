using EVoting.Domain.Enums;

namespace EVoting.Application.Interfaces;

public record JwtResult(string Token, DateTime ExpiresAt);

public interface IJwtTokenService
{
    JwtResult GenerateToken(Guid userId, UserRole role);
}
