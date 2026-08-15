namespace EVoting.Application.Interfaces;

public interface IOtpCodeHasher
{
    string Hash(string code);
    bool Verify(string code, string hash);
}