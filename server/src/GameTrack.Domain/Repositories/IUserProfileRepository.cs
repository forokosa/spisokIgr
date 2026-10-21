using GameTrack.Domain.Entities;

namespace GameTrack.Domain.Repositories;

/// <summary>Абстракция хранилища профиля пользователя.</summary>
public interface IUserProfileRepository
{
    Task<UserProfile?> GetAsync(CancellationToken cancellationToken = default);

    Task<UserProfile> UpdateAsync(UserProfile profile, CancellationToken cancellationToken = default);
}
