using GameTrack.Domain.Common;
using GameTrack.Domain.Enums;
using GameTrack.Domain.Models;

namespace GameTrack.Domain.Entities;

/// <summary>Корневая сущность агрегата — игра в библиотеке пользователя.</summary>
public sealed class Game
{
    private Game(
        Guid id,
        string title,
        string genre,
        string platform,
        GameStatus status,
        string? coverUrl,
        string? description,
        decimal? rating,
        int? releaseYear,
        DateTimeOffset addedAt,
        DateTimeOffset updatedAt)
    {
        Id = Guard.Id(id, nameof(id));
        Title = Guard.NotBlank(title, nameof(title), 2, 200);
        Genre = Guard.NotBlank(genre, nameof(genre), 1, 100);
        Platform = Guard.NotBlank(platform, nameof(platform), 1, 100);
        Status = GuardStatus(status);
        CoverUrl = ValidateCoverUrl(coverUrl);
        Description = Guard.OptionalString(description, nameof(description), 2000);
        Rating = Guard.Rating(rating, nameof(rating));
        ReleaseYear = Guard.ReleaseYear(releaseYear, nameof(releaseYear));
        AddedAt = ValidateDate(addedAt, nameof(addedAt));
        UpdatedAt = ValidateDate(updatedAt, nameof(updatedAt));

        if (UpdatedAt < AddedAt)
        {
            throw new ArgumentException("Дата обновления не может быть раньше даты добавления.", nameof(updatedAt));
        }
    }

    public Guid Id { get; }

    public string Title { get; private set; }

    public string Genre { get; private set; }

    public string Platform { get; private set; }

    public GameStatus Status { get; private set; }

    public string? CoverUrl { get; private set; }

    public string? Description { get; private set; }

    public decimal? Rating { get; private set; }

    public int? ReleaseYear { get; private set; }

    public DateTimeOffset AddedAt { get; }

    public DateTimeOffset UpdatedAt { get; private set; }

    /// <summary>Создаёт новую игру, заполняя служебные даты.</summary>
    public static Game Create(GameDraft draft, DateTimeOffset? now = null)
    {
        Guard.NotNull(draft, nameof(draft));
        var timestamp = now ?? DateTimeOffset.UtcNow;

        return new Game(
            Guid.NewGuid(),
            draft.Title,
            draft.Genre,
            draft.Platform,
            draft.Status,
            draft.CoverUrl,
            draft.Description,
            draft.Rating,
            draft.ReleaseYear,
            timestamp,
            timestamp);
    }

    /// <summary>Восстанавливает игру из хранилища с полной проверкой инвариантов.</summary>
    public static Game Restore(
        Guid id,
        string title,
        string genre,
        string platform,
        GameStatus status,
        string? coverUrl,
        string? description,
        decimal? rating,
        int? releaseYear,
        DateTimeOffset addedAt,
        DateTimeOffset updatedAt)
        => new(id, title, genre, platform, status, coverUrl, description, rating, releaseYear, addedAt, updatedAt);

    /// <summary>Полное обновление данных игры.</summary>
    public void Update(GameDraft draft, DateTimeOffset? now = null)
    {
        Guard.NotNull(draft, nameof(draft));

        var title = Guard.NotBlank(draft.Title, nameof(draft.Title), 2, 200);
        var genre = Guard.NotBlank(draft.Genre, nameof(draft.Genre), 1, 100);
        var platform = Guard.NotBlank(draft.Platform, nameof(draft.Platform), 1, 100);
        var status = GuardStatus(draft.Status);

        Title = title;
        Genre = genre;
        Platform = platform;
        Status = status;
        CoverUrl = ValidateCoverUrl(draft.CoverUrl);
        Description = Guard.OptionalString(draft.Description, nameof(draft.Description), 2000);
        Rating = Guard.Rating(draft.Rating, nameof(draft.Rating));
        ReleaseYear = Guard.ReleaseYear(draft.ReleaseYear, nameof(draft.ReleaseYear));
        UpdatedAt = ValidateDate(now ?? DateTimeOffset.UtcNow, nameof(now));
    }

    /// <summary>Меняет только статус игры.</summary>
    public void ChangeStatus(GameStatus status, DateTimeOffset? now = null)
    {
        Status = GuardStatus(status);
        UpdatedAt = ValidateDate(now ?? DateTimeOffset.UtcNow, nameof(now));
    }

    /// <summary>Приводит игру к черновику (для полного обновления).</summary>
    public GameDraft ToDraft()
        => new(Title, Genre, Platform, Status, CoverUrl, Description, Rating, ReleaseYear);

    private static GameStatus GuardStatus(GameStatus status)
    {
        if (!Enum.IsDefined(status))
        {
            throw new ArgumentOutOfRangeException(nameof(status), "Некорректный статус игры.");
        }

        return status;
    }

    private static string? ValidateCoverUrl(string? coverUrl)
    {
        var value = Guard.OptionalString(coverUrl, nameof(coverUrl), 500);
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
            throw new ArgumentException("Ссылка на обложку имеет недопустимый формат.", nameof(coverUrl));
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
