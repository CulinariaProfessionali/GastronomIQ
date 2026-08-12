using System.Security.Cryptography;
using GastronomIQ.Application.Identity;

namespace GastronomIQ.Infrastructure.Identity;

public sealed class InMemoryPasswordHasher : IPasswordHasher
{
    private const int SaltSize = 16;
    private const int HashSize = 32;
    private const int Iterations = 100_000;

    public string Hash(string password)
    {
        var salt = RandomNumberGenerator.GetBytes(SaltSize);
        var hash = Rfc2898DeriveBytes.Pbkdf2(
            password,
            salt,
            Iterations,
            HashAlgorithmName.SHA256,
            HashSize);

        return $"v1.{Convert.ToBase64String(salt)}.{Convert.ToBase64String(hash)}";
    }

    public bool Verify(string password, string passwordHash)
    {
        var segments = passwordHash.Split('.');
        if (segments.Length != 3 || segments[0] != "v1")
            return false;

        try
        {
            var salt = Convert.FromBase64String(segments[1]);
            var expectedHash = Convert.FromBase64String(segments[2]);
            var actualHash = Rfc2898DeriveBytes.Pbkdf2(
                password,
                salt,
                Iterations,
                HashAlgorithmName.SHA256,
                expectedHash.Length);

            return CryptographicOperations.FixedTimeEquals(actualHash, expectedHash);
        }
        catch (FormatException)
        {
            return false;
        }
    }
}
