using System.Security.Cryptography;
using System.Text;
using EVoting.Application.Interfaces;

namespace EVoting.Infrastructure.Security;

public class Sha256VoterAnonymizer : IVoterAnonymizer
{
    public string ComputeVoterId(Guid userId)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(userId.ToString()));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }
}
