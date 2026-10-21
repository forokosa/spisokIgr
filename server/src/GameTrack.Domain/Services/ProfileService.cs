using GameTrack.Domain.Common;
using GameTrack.Domain.Entities;
using GameTrack.Domain.Models;
using GameTrack.Domain.Repositories;

namespace GameTrack.Domain.Services;

/// <summary>Доменный сервис профиля пользователя.</summary>
public sealed class ProfileService
{
    private readonly IUserProfileRepository _repository;

    public ProfileService(IUserProfileRepository repository)
    {
        _repository = Guard.NotNull(repository, nameof(repository));
    }

    public async Task<UserProfile> GetAsync(CancellationToken cancellationToken = default)
    {
        return await _repository.GetAsync(cancellationToken)
               ?? throw new NotFoundException("Профиль пользователя не найден.");
    }

    public async Task<UserProfile> UpdateAsync(
        ProfileDraft draft,
        CancellationToken cancellationToken = default)
    {
        Guard.NotNull(draft, nameof(draft));

        var profile = await GetAsync(cancellationToken);
        profile.Update(draft);

        return await _repository.UpdateAsync(profile, cancellationToken);
    }
}
