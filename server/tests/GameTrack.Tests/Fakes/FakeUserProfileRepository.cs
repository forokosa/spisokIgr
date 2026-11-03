using GameTrack.Domain.Common;
using GameTrack.Domain.Entities;
using GameTrack.Domain.Models;
using GameTrack.Domain.Repositories;

namespace GameTrack.Tests.Fakes;

/// <summary>In-memory профили для unit-тестов (в рабочем коде отсутствует).</summary>
public sealed class FakeUserProfileRepository : IUserProfileRepository
{
    private readonly object _sync = new();
    private readonly Dictionary<Guid, UserProfile> _profiles = [];

    public FakeUserProfileRepository()
    {
    }

    public FakeUserProfileRepository(Guid userId, UserProfile seed)
    {
        _profiles[userId] = Clone(seed);
    }

    public async Task<UserProfile?> GetAsync(
        UserContext user,
        CancellationToken cancellationToken = default)
    {
        Guard.NotNull(user, nameof(user));
        await Task.CompletedTask;
        cancellationToken.ThrowIfCancellationRequested();

        lock (_sync)
        {
            return _profiles.TryGetValue(user.UserId, out var profile) ? Clone(profile) : null;
        }
    }

    public async Task<UserProfile> UpdateAsync(
        UserContext user,
        UserProfile profile,
        CancellationToken cancellationToken = default)
    {
        Guard.NotNull(user, nameof(user));
        Guard.NotNull(profile, nameof(profile));
        await Task.CompletedTask;
        cancellationToken.ThrowIfCancellationRequested();

        lock (_sync)
        {
            if (!_profiles.ContainsKey(user.UserId))
            {
                throw new NotFoundException("Профиль пользователя не найден.");
            }

            if (profile.Id != user.UserId)
            {
                throw new ArgumentException("Идентификатор профиля не совпадает с текущим пользователем.", nameof(profile));
            }

            _profiles[user.UserId] = Clone(profile);
            return Clone(profile);
        }
    }

    public async Task CreateAsync(UserProfile profile, CancellationToken cancellationToken = default)
    {
        Guard.NotNull(profile, nameof(profile));
        await Task.CompletedTask;
        cancellationToken.ThrowIfCancellationRequested();

        lock (_sync)
        {
            _profiles.TryAdd(profile.Id, Clone(profile));
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
