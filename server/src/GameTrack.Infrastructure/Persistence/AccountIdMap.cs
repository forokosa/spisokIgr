using System.Buffers.Binary;
using System.Security.Cryptography;
using System.Text;
using GameTrack.Domain.Common;

namespace GameTrack.Infrastructure.Persistence;

/// <summary>
/// Мост между публичным идентификатором аккаунта (Guid — заголовок X-User-Id
/// и контракты API) и техническим ключом БД (accounts.id bigint identity).
///
/// Ключи БД не меняются. Для логина вычисляется детерминированный UUID:
/// <code>
///   guid[0..3]  = длина логина в UTF-8 (Big-Endian Int32)
///   guid[4..15] = SHA-256("gametrack.account:" + логин)[0..12]
/// </code>
///
/// Почему так:
///   * Guid стабилен и одинаков на всех серверах — соответствие не нужно хранить;
///   * по длине логина (первые 4 байта) PostgreSQL находит кандидатов
///     через индекс по length(login), не перебирая таблицу;
///   * итоговую проверку всё равно делает сравнение SHA-256, поэтому
///     коллизии хеша невозможны на практике, а длина — только подсказка.
/// </summary>
public static class AccountIdMap
{
    /// <summary>Логин, под которым хранится демонстрационный профиль.</summary>
    public const string DemoLogin = "demo";

    /// <summary>Публичный идентификатор демо-профиля.</summary>
    public static readonly Guid DemoUserId = ToPublicId(DemoLogin);

    private const string HashPrefix = "gametrack.account:";
    private const int LengthBytes = 4;
    private const int HashBytes = 16 - LengthBytes;

    /// <summary>Логин аккаунта -> стабильный Guid.</summary>
    public static Guid ToPublicId(string login)
    {
        var normalized = NormalizeLogin(login);
        var loginBytes = Encoding.UTF8.GetBytes(normalized);
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(HashPrefix + normalized));

        Span<byte> bytes = stackalloc byte[16];
        BinaryPrimitives.WriteInt32BigEndian(bytes[..LengthBytes], loginBytes.Length);
        hash.AsSpan(0, HashBytes).CopyTo(bytes[LengthBytes..]);

        return new Guid(bytes, bigEndian: true);
    }

    /// <summary>Длина логина, закодированная в Guid (подсказка для поиска в БД).</summary>
    public static int GetLoginLength(Guid publicId)
    {
        Span<byte> bytes = stackalloc byte[16];
        publicId.TryWriteBytes(bytes, bigEndian: true, out _);
        return BinaryPrimitives.ReadInt32BigEndian(bytes[..LengthBytes]);
    }

    /// <summary>Проверяет соответствие логина и публичного идентификатора.</summary>
    public static bool Matches(string login, Guid publicId) => ToPublicId(login) == publicId;

    /// <summary>
    /// Выводит логин из имени и email для нового аккаунта: API прежних версий
    /// отдельного логина не принимало, а accounts.login обязателен и уникален.
    /// </summary>
    public static string CreateLogin(string name, string email)
    {
        var validEmail = Guard.Email(email, nameof(email));

        var candidate = new string(
            validEmail.TakeWhile(character => character != '@')
                .Where(character => char.IsAsciiLetterOrDigit(character) || character is '.' or '_' or '-')
                .ToArray());

        if (candidate.Length < 3)
        {
            var safeName = new string(
                Guard.NotBlank(name, nameof(name), 2, 100)
                    .Where(char.IsAsciiLetterOrDigit)
                    .ToArray());

            candidate = $"{safeName}user";
            if (candidate.Length < 3)
            {
                candidate = "player";
            }
        }

        return candidate.Length <= 50 ? candidate : candidate[..50];
    }

    /// <summary>Резервный логин при конфликте: <c>base_ab12cd</c>.</summary>
    public static string CreateSuffixedLogin(string login)
    {
        var suffix = Convert.ToHexString(RandomNumberGenerator.GetBytes(3)).ToLowerInvariant();
        var basePart = login.Length > 42 ? login[..42] : login;
        return $"{basePart}_{suffix}";
    }

    private static string NormalizeLogin(string login)
    {
        var value = Guard.NotBlank(login, nameof(login), 1, 50);
        return value.ToLowerInvariant();
    }
}
