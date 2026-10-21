using GameTrack.Domain.Common;
using GameTrack.Domain.Entities;
using GameTrack.Domain.Repositories;

namespace GameTrack.Infrastructure.Persistence;

/// <summary>In-memory заглушка репозитория профиля пользователя.</summary>
public sealed class InMemoryUserProfileRepository : IUserProfileRepository
{
    private readonly object _sync = new();
    private UserProfile _profile;

    public InMemoryUserProfileRepository() : this(SeedData.CreateProfile())
    {
    }

    public InMemoryUserProfileRepository(UserProfile seed)
    {
        _profile = Clone(Guard.NotNull(seed, nameof(seed)));
    }

    public async Task<UserProfile?> GetAsync(CancellationToken cancellationToken = default)
    {
        await Task.CompletedTask;
        cancellationToken.ThrowIfCancellationRequested();

        lock (_sync)
        {
            return Clone(_profile);
        }
    }

    public async Task<UserProfile> UpdateAsync(
        UserProfile profile,
        CancellationToken cancellationToken = default)
    {
        Guard.NotNull(profile, nameof(profile));
        await Task.CompletedTask;
        cancellationToken.ThrowIfCancellationRequested();

        lock (_sync)
        {
            if (profile.Id != _profile.Id)
            {
                throw new ArgumentException("Идентификатор профиля не совпадает с хранимым.", nameof(profile));
            }

            _profile = Clone(profile);
            return Clone(_profile);
        }
    }

    private static UserProfile Clone(UserProfile profile) => UserProfile.Restore(
        profile.Id,
        profile.Name,
        profile.Email,
        profile.AvatarUrl,
        profile.CreatedAt,
        profile.UpdatedAt);
}
