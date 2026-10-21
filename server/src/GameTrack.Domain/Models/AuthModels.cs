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

/// <summary>Зарегистрированный аккаунт без секретных данных.</summary>
public sealed record UserAccount(Guid Id, string Name, string Email);

/// <summary>Данные для создания нового аккаунта.</summary>
public sealed record RegisterDraft(
    string Name,
    string Email,
    string Password,
    bool Remember = false);
