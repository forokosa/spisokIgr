using System.Globalization;
using System.Security.Cryptography;
using System.Text;

namespace GameTrack.Infrastructure.Security;

/// <summary>
/// Хеширование паролей по схеме PBKDF2-HMAC-SHA256.
///
/// В БД <c>accounts.password_hash</c> — одна строка (varchar(255)), поэтому
/// соль, число итераций и хеш хранятся вместе:
/// <code>pbkdf2-sha256$&lt;итерации&gt;$&lt;соль base64&gt;$&lt;хеш base64&gt;</code>
/// Формат совпадает с данными из db/003_seed_demo_data.sql.
///
/// Сравнение выполняется за постоянное время, чтобы пароль нельзя было
/// подобрать по времени ответа.
/// </summary>
public static class PasswordHasher
{
    private const string Prefix = "pbkdf2-sha256";
    private const int SaltSize = 16;
    private const int HashSize = 32;
    private const int Iterations = 100_000;

    private const char Separator = '$';

    /// <summary>Возвращает строку для хранения в accounts.password_hash.</summary>
    public static string Hash(string password)
    {
        ArgumentException.ThrowIfNullOrEmpty(password);

        var salt = RandomNumberGenerator.GetBytes(SaltSize);
        var hash = Derive(password, salt, Iterations, HashSize);

        return string.Join(
            Separator,
            Prefix,
            Iterations.ToString(CultureInfo.InvariantCulture),
            Convert.ToBase64String(salt),
            Convert.ToBase64String(hash));
    }

    /// <summary>
    /// Проверяет пароль по сохранённой строке. Некорректный формат считается
    /// несовпадением: битые данные в БД не должны превращаться в исключение 500.
    /// </summary>
    public static bool Verify(string password, string? storedHash)
    {
        if (string.IsNullOrEmpty(password) || string.IsNullOrWhiteSpace(storedHash))
        {
            return false;
        }

        var parts = storedHash.Split(Separator);
        if (parts.Length != 4 || parts[0] != Prefix)
        {
            return false;
        }

        if (!int.TryParse(parts[1], NumberStyles.None, CultureInfo.InvariantCulture, out var iterations)
            || iterations <= 0)
        {
            return false;
        }

        byte[] salt;
        byte[] expected;
        try
        {
            salt = Convert.FromBase64String(parts[2]);
            expected = Convert.FromBase64String(parts[3]);
        }
        catch (FormatException)
        {
            return false;
        }

        if (salt.Length == 0 || expected.Length == 0)
        {
            return false;
        }

        var actual = Derive(password, salt, iterations, expected.Length);
        return CryptographicOperations.FixedTimeEquals(actual, expected);
    }

    private static byte[] Derive(string password, byte[] salt, int iterations, int length)
        => Rfc2898DeriveBytes.Pbkdf2(
            Encoding.UTF8.GetBytes(password),
            salt,
            iterations,
            HashAlgorithmName.SHA256,
            length);
}
