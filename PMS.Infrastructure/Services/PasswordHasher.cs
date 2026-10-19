using PMS.Application.Common.Interfaces;
using PMS.Domain.Constants;
using System.Security.Cryptography;
using System.Text;

namespace PMS.Infrastructure.Services;

/// <summary>PBKDF2-SHA256, stored as "PBKDF2-SHA256$iterations$saltB64$hashB64".</summary>
public class PasswordHasher : IPasswordHasher
{
    public string Hash(string password)
    {
        var salt = RandomNumberGenerator.GetBytes(AppConstants.PasswordSaltBytes);
        var key = Derive(password, salt, AppConstants.PasswordHashIterations, AppConstants.PasswordKeyBytes);
        return $"{AppConstants.PasswordHashPrefix}${AppConstants.PasswordHashIterations}${Convert.ToBase64String(salt)}${Convert.ToBase64String(key)}";
    }

    public bool Verify(string password, string? storedHash)
    {
        // Always do the expensive derivation, even with no/garbled stored hash, so response
        // time does not reveal whether an account exists.
        var parts = storedHash?.Split('$');
        var valid = parts is { Length: 4 } && parts[0] == AppConstants.PasswordHashPrefix
                    && int.TryParse(parts[1], out _);

        var iterations = valid ? int.Parse(parts![1]) : AppConstants.PasswordHashIterations;
        var salt = valid ? Convert.FromBase64String(parts![2]) : new byte[AppConstants.PasswordSaltBytes];
        var expected = valid ? Convert.FromBase64String(parts![3]) : new byte[AppConstants.PasswordKeyBytes];

        var actual = Derive(password, salt, iterations, expected.Length);
        return CryptographicOperations.FixedTimeEquals(actual, expected) && valid;
    }

    private static byte[] Derive(string password, byte[] salt, int iterations, int length) =>
        Rfc2898DeriveBytes.Pbkdf2(Encoding.UTF8.GetBytes(password), salt, iterations, HashAlgorithmName.SHA256, length);
}
