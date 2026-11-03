using GameTrack.Domain.Common;
using GameTrack.Domain.Entities;
using GameTrack.Domain.Models;
using GameTrack.Domain.Repositories;

namespace GameTrack.Domain.Services;

/// <summary>Доменный сервис профиля пользователя.</summary>
public sealed class ProfileService
{
    private readonly IUserProfileRepository _repository;
    private readonly IUserContextProvider _users;

    public ProfileService(IUserProfileRepository repository, IUserContextProvider users)
    {
        _repository = Guard.NotNull(repository, nameof(repository));
        _users = Guard.NotNull(users, nameof(users));
    }

    public async Task<UserProfile> GetAsync(CancellationToken cancellationToken = default)
    {
        return await _repository.GetAsync(_users.GetRequiredUser(), cancellationToken)
               ?? throw new NotFoundException("Профиль пользователя не найден.");
    }

    public async Task<UserProfile> UpdateAsync(
        ProfileDraft draft,
        CancellationToken cancellationToken = default)
    {
        Guard.NotNull(draft, nameof(draft));

        var user = _users.GetRequiredUser();
        var profile = await _repository.GetAsync(user, cancellationToken)
                      ?? throw new NotFoundException("Профиль пользователя не найден.");

        profile.Update(draft);

        return await _repository.UpdateAsync(user, profile, cancellationToken);
    }
}
