using GameTrack.Domain.Common;
using GameTrack.Domain.Models;
using GameTrack.Domain.Repositories;
using GameTrack.Infrastructure.Persistence;
using GameTrack.Infrastructure.Security;

namespace GameTrack.Tests.Fakes;

/// <summary>In-memory аккаунты для unit-тестов (в рабочем коде отсутствует).</summary>
public sealed class FakeAccountRepository : IAccountRepository
{
    private readonly object _sync = new();
    private readonly Dictionary<string, AccountState> _accounts = new(StringComparer.OrdinalIgnoreCase);

    public FakeAccountRepository()
    {
    }

    public FakeAccountRepository(string email, string password, string name = "Алексей Игрок", Guid? id = null)
    {
        var normalizedEmail = Guard.Email(email, nameof(email));
        var login = AccountIdMap.CreateLogin(name, normalizedEmail);

        _accounts[normalizedEmail] = new AccountState(
            id ?? AccountIdMap.ToPublicId(login),
            name,
            normalizedEmail,
            login,
            PasswordHasher.Hash(Guard.Password(password, nameof(password))));
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
                ? new UserAccount(account.Id, account.Name, account.Email, account.Login)
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
        var login = AccountIdMap.CreateLogin(validName, normalizedEmail);

        await Task.CompletedTask;
        cancellationToken.ThrowIfCancellationRequested();

        lock (_sync)
        {
            if (_accounts.ContainsKey(normalizedEmail))
            {
                throw new ConflictException("Аккаунт с таким email уже существует.");
            }

            var account = new AccountState(
                AccountIdMap.ToPublicId(login),
                validName,
                normalizedEmail,
                login,
                PasswordHasher.Hash(validPassword));

            _accounts[normalizedEmail] = account;
            return new UserAccount(account.Id, account.Name, account.Email, account.Login);
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

        lock (_sync)
        {
            return _accounts.TryGetValue(normalizedEmail, out var account)
                   && PasswordHasher.Verify(password, account.PasswordHash);
        }
    }

    private sealed record AccountState(Guid Id, string Name, string Email, string Login, string PasswordHash);
}
