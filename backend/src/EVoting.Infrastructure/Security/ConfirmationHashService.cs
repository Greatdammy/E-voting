using System.Security.Cryptography;
using System.Text;
using EVoting.Application.Interfaces;
using Microsoft.Extensions.Configuration;

namespace EVoting.Infrastructure.Security;

public class ConfirmationHashService : IConfirmationHashService
{
    private readonly IConfiguration _configuration;

    public ConfirmationHashService(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public string Compute(Guid voteId, Guid electionId)
    {
        var secret = _configuration["Voting:ConfirmationSecret"]
            ?? throw new InvalidOperationException("Voting:ConfirmationSecret is not configured.");

        var keyBytes = Encoding.UTF8.GetBytes(secret);
        var messageBytes = Encoding.UTF8.GetBytes($"{voteId}:{electionId}");

        var hashBytes = HMACSHA256.HashData(keyBytes, messageBytes);

        return Convert.ToHexString(hashBytes).ToLowerInvariant();
    }
}
