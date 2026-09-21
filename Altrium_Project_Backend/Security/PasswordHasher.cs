// written by malan
using System.Security.Cryptography;

namespace Altrium_Project_Backend.Security
{
    public interface IPasswordHasher
    {
        // Hashes a plaintext password for storage in dbo.[User].password_hash.
        string Hash(string password);

        // Re-hashes the supplied password and compares it with the stored value.
        // Returns false for any stored value that is not in our format, which is
        // what makes the pre-authentication rows (plain text, empty) unusable.
        bool Verify(string password, string? storedHash);
    }

    // PBKDF2 (RFC 2898) with HMAC-SHA256 - the same algorithm family ASP.NET Core
    // Identity uses. Deliberately slow, and salted per user, so a leaked table cannot
    // be attacked with rainbow tables and brute force stays expensive.
    //
    // Stored format:  PBKDF2$<iterations>$<base64 salt>$<base64 hash>
    // Everything needed to verify travels inside the single column, so the work
    // factor can be raised later without a migration.
    public class PasswordHasher : IPasswordHasher
    {
        private const string Prefix = "PBKDF2";
        private const int SaltBytes = 16;
        private const int HashBytes = 32;
        private const int Iterations = 120_000;

        public string Hash(string password)
        {
            if (string.IsNullOrWhiteSpace(password))
                throw new ArgumentException("Password must not be empty.", nameof(password));

            var salt = RandomNumberGenerator.GetBytes(SaltBytes);
            var hash = Derive(password, salt, Iterations);
            return $"{Prefix}${Iterations}${Convert.ToBase64String(salt)}${Convert.ToBase64String(hash)}";
        }

        public bool Verify(string password, string? storedHash)
        {
            if (string.IsNullOrWhiteSpace(password) || string.IsNullOrWhiteSpace(storedHash)) return false;

            var parts = storedHash.Split('$');
            if (parts.Length != 4 || parts[0] != Prefix) return false;            // legacy / unusable value
            if (!int.TryParse(parts[1], out var iterations) || iterations < 1000) return false;

            byte[] salt, expected;
            try
            {
                salt = Convert.FromBase64String(parts[2]);
                expected = Convert.FromBase64String(parts[3]);
            }
            catch (FormatException)
            {
                return false;
            }

            var actual = Derive(password, salt, iterations, expected.Length);
            // Fixed-time comparison: a plain == would leak how much of the hash matched.
            return CryptographicOperations.FixedTimeEquals(actual, expected);
        }

        private static byte[] Derive(string password, byte[] salt, int iterations, int length = HashBytes) =>
            Rfc2898DeriveBytes.Pbkdf2(password, salt, iterations, HashAlgorithmName.SHA256, length);
    }
}
