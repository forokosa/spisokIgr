using GameTrack.Domain.Common;
using GameTrack.Domain.Models;

namespace GameTrack.Domain.Entities;

/// <summary>Профиль пользователя приложения.</summary>
public sealed class UserProfile
{
    private UserProfile(
        Guid id,
        string name,
        string email,
        string? avatarUrl,
        DateTimeOffset createdAt,
        DateTimeOffset updatedAt)
    {
        Id = Guard.Id(id, nameof(id));
        Name = Guard.NotBlank(name, nameof(name), 2, 100);
        Email = Guard.Email(email, nameof(email));
        AvatarUrl = ValidateAvatarUrl(avatarUrl);
        CreatedAt = ValidateDate(createdAt, nameof(createdAt));
        UpdatedAt = ValidateDate(updatedAt, nameof(updatedAt));

        if (UpdatedAt < CreatedAt)
        {
            throw new ArgumentException("Дата обновления не может быть раньше даты создания.", nameof(updatedAt));
        }
    }

    public Guid Id { get; }

    public string Name { get; private set; }

    public string Email { get; private set; }

    public string? AvatarUrl { get; private set; }

    public DateTimeOffset CreatedAt { get; }

    public DateTimeOffset UpdatedAt { get; private set; }

    public static UserProfile Restore(
        Guid id,
        string name,
        string email,
        string? avatarUrl,
        DateTimeOffset createdAt,
        DateTimeOffset updatedAt)
        => new(id, name, email, avatarUrl, createdAt, updatedAt);

    /// <summary>Обновляет редактируемые поля профиля.</summary>
    public void Update(ProfileDraft draft, DateTimeOffset? now = null)
    {
        Guard.NotNull(draft, nameof(draft));

        Name = Guard.NotBlank(draft.Name, nameof(draft.Name), 2, 100);
        Email = Guard.Email(draft.Email, nameof(draft.Email));
        AvatarUrl = ValidateAvatarUrl(draft.AvatarUrl);
        UpdatedAt = ValidateDate(now ?? DateTimeOffset.UtcNow, nameof(now));

        if (UpdatedAt < CreatedAt)
        {
            throw new ArgumentException("Дата обновления не может быть раньше даты создания.", nameof(now));
        }
    }

    private static string? ValidateAvatarUrl(string? avatarUrl)
    {
        var value = Guard.OptionalString(avatarUrl, nameof(avatarUrl), 500);
        if (value is null)
        {
            return null;
        }

        var isValid = value.StartsWith("images/", StringComparison.OrdinalIgnoreCase)
                      || value.StartsWith("idb:", StringComparison.OrdinalIgnoreCase)
                      || value.StartsWith("mem:", StringComparison.OrdinalIgnoreCase)
                      || Uri.TryCreate(value, UriKind.Absolute, out var uri)
                          && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps);

        if (!isValid)
        {
            throw new ArgumentException("Ссылка на аватар имеет недопустимый формат.", nameof(avatarUrl));
        }

        return value;
    }

    private static DateTimeOffset ValidateDate(DateTimeOffset value, string paramName)
    {
        if (value == default)
        {
            throw new ArgumentException("Дата не может быть значением по умолчанию.", paramName);
        }

        if (value.Year < 2000 || value.Year > 2200)
        {
            throw new ArgumentOutOfRangeException(paramName, "Дата находится вне допустимого диапазона.");
        }

        return value.ToUniversalTime();
    }
}
