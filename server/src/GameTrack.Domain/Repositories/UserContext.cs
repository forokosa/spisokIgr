using GameTrack.Domain.Common;

namespace GameTrack.Domain.Repositories;

/// <summary>
/// Кто выполняет запрос. <see cref="UserId"/> — публичный идентификатор
/// (Guid из заголовка X-User-Id), <see cref="Login"/> — технический логин
/// аккаунта, через который Guid отображается в ключ БД. Откуда берутся оба
/// значения, доменный слой не знает: это забота Presentation.
/// </summary>
public sealed record UserContext
{
    public UserContext(Guid userId, string login)
    {
        UserId = Guard.Id(userId, nameof(userId));
        Login = Guard.NotBlank(login, nameof(login), 1, 50);
    }

    public Guid UserId { get; }

    /// <summary>Технический логин: ключ accounts.login, в API не отдаётся.</summary>
    public string Login { get; }
}
