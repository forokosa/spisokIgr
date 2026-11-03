namespace GameTrack.Domain.Models;

/// <summary>Сессия пользователя, выданная после успешного входа.</summary>
public sealed record AuthSession(
    Guid UserId,
    string Email,
    string Name,
    DateTimeOffset IssuedAt,
    bool Remember);

/// <summary>Результат запроса восстановления пароля.</summary>
public sealed record RecoveryResult(string Email, bool Delivered, string Code);

/// <summary>
/// Зарегистрированный аккаунт без секретных данных.
/// <paramref name="Login"/> — технический логин (уникальный ключ в БД),
/// в API не отдаётся: клиент по-прежнему входит по email.
/// </summary>
public sealed record UserAccount(Guid Id, string Name, string Email, string Login = "")
{
    public string Login { get; init; } = string.IsNullOrWhiteSpace(Login) ? Email : Login;
}

/// <summary>Данные для создания нового аккаунта.</summary>
public sealed record RegisterDraft(
    string Name,
    string Email,
    string Password,
    bool Remember = false);
