using System.Security.Cryptography;
using System.Text;
using EVoting.Application.Interfaces;

namespace EVoting.Infrastructure.Security;

public class Sha256OtpHasher : IOtpCodeHasher
{
    public string Hash(string code)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(code));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }

    public bool Verify(string code, string hash)
    {
        var computedBytes = Encoding.UTF8.GetBytes(Hash(code));
        var expectedBytes = Encoding.UTF8.GetBytes(hash);

        if (computedBytes.Length != expectedBytes.Length)
        {
            return false;
        }

        return CryptographicOperations.FixedTimeEquals(computedBytes, expectedBytes);
    }
}