using System.Security.Cryptography;
using System.Text;
using GameTrack.Domain.Common;
using GameTrack.Domain.Models;
using GameTrack.Domain.Repositories;

namespace GameTrack.Infrastructure.Security;

/// <summary>
/// In-memory хранилище аккаунтов. Пароли не хранятся в открытом виде:
/// используются PBKDF2, случайная соль и фиксированное число итераций.
/// </summary>
public sealed class InMemoryAccountRepository : IAccountRepository
{
    private const int SaltSize = 16;
    private const int HashSize = 32;
    private const int Iterations = 100_000;

    private readonly object _sync = new();
    private readonly Dictionary<string, AccountRecord> _accounts =
        new(StringComparer.OrdinalIgnoreCase);

    /// <summary>Создаёт пустое хранилище (регистрация доступна сразу).</summary>
    public InMemoryAccountRepository()
    {
    }

    /// <summary>Создаёт хранилище с заранее добавленным демонстрационным аккаунтом.</summary>
    public InMemoryAccountRepository(
        string email,
        string password,
        string name = "Алексей Игрок",
        Guid? id = null)
    {
        var normalizedEmail = Guard.Email(email, nameof(email));
        var validName = Guard.NotBlank(name, nameof(name), 2, 100);
        var validPassword = Guard.Password(password, nameof(password));
        var accountId = id ?? Guid.NewGuid();
        Guard.Id(accountId, nameof(id));
        var (hash, salt) = HashPassword(validPassword);

        _accounts[normalizedEmail] = new AccountRecord(
            accountId,
            validName,
            normalizedEmail,
            hash,
            salt);
    }

    public async Task<bool> ExistsAsync(string email, CancellationToken cancellationToken = default)
    {
        var normalizedEmail = Guard.Email(email, nameof(email));
        await Task.CompletedTask;
        cancellationToken.ThrowIfCancellationRequested();

        lock (_sync)
        {
            return _accounts.ContainsKey(normalizedEmail);
        }
    }

    public async Task<UserAccount?> FindAsync(string email, CancellationToken cancellationToken = default)
    {
        var normalizedEmail = Guard.Email(email, nameof(email));
        await Task.CompletedTask;
        cancellationToken.ThrowIfCancellationRequested();

        lock (_sync)
        {
            return _accounts.TryGetValue(normalizedEmail, out var account)
                ? new UserAccount(account.Id, account.Name, account.Email)
                : null;
        }
    }

    public async Task<UserAccount> RegisterAsync(
        string name,
        string email,
        string password,
        CancellationToken cancellationToken = default)
    {
        var validName = Guard.NotBlank(name, nameof(name), 2, 100);
        var normalizedEmail = Guard.Email(email, nameof(email));
        var validPassword = Guard.Password(password, nameof(password));
        var (hash, salt) = HashPassword(validPassword);

        await Task.CompletedTask;
        cancellationToken.ThrowIfCancellationRequested();

        lock (_sync)
        {
            if (_accounts.ContainsKey(normalizedEmail))
            {
                throw new ConflictException("Аккаунт с таким email уже существует.");
            }

            var account = new AccountRecord(
                Guid.NewGuid(),
                validName,
                normalizedEmail,
                hash,
                salt);
            _accounts[normalizedEmail] = account;
            return new UserAccount(account.Id, account.Name, account.Email);
        }
    }

    public async Task<bool> ValidateAsync(
        string email,
        string password,
        CancellationToken cancellationToken = default)
    {
        var normalizedEmail = Guard.Email(email, nameof(email));
        if (password is null)
        {
            throw new ArgumentNullException(nameof(password), "Пароль обязателен.");
        }

        if (password.Length is < 1 or > 128)
        {
            throw new ArgumentOutOfRangeException(nameof(password), "Пароль должен содержать от 1 до 128 символов.");
        }

        await Task.CompletedTask;
        cancellationToken.ThrowIfCancellationRequested();

        AccountRecord? account;
        lock (_sync)
        {
            if (!_accounts.TryGetValue(normalizedEmail, out account))
            {
                return false;
            }
        }

        var candidate = DeriveHash(password, Convert.FromBase64String(account.Salt));
        var expected = Convert.FromBase64String(account.PasswordHash);
        return CryptographicOperations.FixedTimeEquals(candidate, expected);
    }

    private static (string Hash, string Salt) HashPassword(string password)
    {
        var salt = RandomNumberGenerator.GetBytes(SaltSize);
        var hash = DeriveHash(password, salt);
        return (Convert.ToBase64String(hash), Convert.ToBase64String(salt));
    }

    private static byte[] DeriveHash(string password, byte[] salt)
        => Rfc2898DeriveBytes.Pbkdf2(
            Encoding.UTF8.GetBytes(password),
            salt,
            Iterations,
            HashAlgorithmName.SHA256,
            HashSize);

    private sealed record AccountRecord(
        Guid Id,
        string Name,
        string Email,
        string PasswordHash,
        string Salt);
}
