using EVoting.Application.Interfaces;
using BCryptNet = BCrypt.Net.BCrypt;

namespace EVoting.Infrastructure.Security;

public class BCryptPasswordHasher : IPasswordHasher
{
    private const int WorkFactor = 12;

    public string Hash(string value) => BCryptNet.HashPassword(value, workFactor: WorkFactor);

    public bool Verify(string value, string hash) => BCryptNet.Verify(value, hash);
}
