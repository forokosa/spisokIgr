using GameTrack.Domain.Entities;

namespace GameTrack.Domain.Repositories;

/// <summary>
/// Профиль пользователя. Все операции адресуются <see cref="UserContext"/>:
/// чужой профиль прочитать или изменить нельзя.
/// </summary>
public interface IUserProfileRepository
{
    /// <summary>Возвращает профиль пользователя или <c>null</c>.</summary>
    Task<UserProfile?> GetAsync(UserContext user, CancellationToken cancellationToken = default);

    /// <summary>Обновляет имя, email и аватар профиля.</summary>
    Task<UserProfile> UpdateAsync(
        UserContext user,
        UserProfile profile,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Сохраняет профиль только что зарегистрированного аккаунта. Доменный слой
    /// не знает технического ключа БД, поэтому аккаунт ищется по email.
    /// </summary>
    Task CreateAsync(UserProfile profile, CancellationToken cancellationToken = default);
}
