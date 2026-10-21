using GameTrack.Domain.Common;
using GameTrack.Domain.Entities;
using GameTrack.Domain.Models;
using GameTrack.Domain.Repositories;

namespace GameTrack.Domain.Services;

/// <summary>Доменный сервис регистрации, аутентификации и восстановления доступа.</summary>
public sealed class AuthService
{
    private readonly IUserProfileRepository _profiles;
    private readonly IAccountRepository _accounts;

    public AuthService(
        IUserProfileRepository profiles,
        IAccountRepository accounts)
    {
        _profiles = Guard.NotNull(profiles, nameof(profiles));
        _accounts = Guard.NotNull(accounts, nameof(accounts));
    }

    /// <summary>Создаёт новый аккаунт и сразу возвращает сессию.</summary>
    public async Task<AuthSession> RegisterAsync(
        RegisterDraft draft,
        CancellationToken cancellationToken = default)
    {
        Guard.NotNull(draft, nameof(draft));

        var name = Guard.NotBlank(draft.Name, nameof(draft.Name), 2, 100);
        var email = Guard.Email(draft.Email, nameof(draft.Email));
        var password = Guard.Password(draft.Password, nameof(draft.Password));

        if (await _accounts.ExistsAsync(email, cancellationToken))
        {
            throw new ConflictException("Аккаунт с таким email уже существует.");
        }

        var account = await _accounts.RegisterAsync(name, email, password, cancellationToken);
        var profile = await GetOrCreateProfileAsync(account, cancellationToken);

        return new AuthSession(
            account.Id,
            account.Email,
            account.Name,
            DateTimeOffset.UtcNow,
            draft.Remember);
    }

    public async Task<AuthSession> LoginAsync(
        string? email,
        string? password,
        bool remember,
        CancellationToken cancellationToken = default)
    {
        var normalizedEmail = Guard.Email(email, nameof(email));
        if (password is null)
        {
            throw new ArgumentNullException(nameof(password), "Пароль обязателен.");
        }

        if (password.Length is < 4 or > 128)
        {
            throw new ArgumentOutOfRangeException(nameof(password), "Пароль должен содержать от 4 до 128 символов.");
        }

        var isValid = await _accounts.ValidateAsync(normalizedEmail, password, cancellationToken);
        var account = isValid
            ? await _accounts.FindAsync(normalizedEmail, cancellationToken)
            : null;
        if (account is null)
        {
            throw new UnauthorizedException("Неверный email или пароль.");
        }

        var profile = await GetOrCreateProfileAsync(account, cancellationToken);

        return new AuthSession(
            account.Id,
            account.Email,
            account.Name,
            DateTimeOffset.UtcNow,
            remember);
    }

    public async Task<RecoveryResult> RequestRecoveryAsync(
        string? email,
        CancellationToken cancellationToken = default)
    {
        var normalizedEmail = Guard.Email(email, nameof(email));
        _ = await _accounts.FindAsync(normalizedEmail, cancellationToken)
            ?? throw new NotFoundException("Аккаунт с таким email не найден.");

        var code = Random.Shared.Next(100000, 999999).ToString();
        return new RecoveryResult(normalizedEmail, true, code);
    }

    /// <summary>
    /// Возвращает профиль активного пользователя. В демонстрационном приложении
    /// профиль один, поэтому при входе под другим аккаунтом его данные обновляются.
    /// </summary>
    private async Task<UserProfile> GetOrCreateProfileAsync(
        UserAccount account,
        CancellationToken cancellationToken)
    {
        var profile = await _profiles.GetAsync(cancellationToken)
                      ?? throw new NotFoundException("Профиль пользователя не найден.");

        if (!string.Equals(profile.Email, account.Email, StringComparison.OrdinalIgnoreCase)
            || !string.Equals(profile.Name, account.Name, StringComparison.Ordinal))
        {
            profile.Update(new ProfileDraft(account.Name, account.Email, profile.AvatarUrl));
            profile = await _profiles.UpdateAsync(profile, cancellationToken);
        }

        return profile;
    }
}
