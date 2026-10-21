using System.ComponentModel.DataAnnotations;
using GameTrack.Domain.Entities;
using GameTrack.Domain.Enums;
using GameTrack.Domain.Models;

namespace GameTrack.Api.Contracts;

/// <summary>Игра, возвращаемая клиенту.</summary>
public sealed record GameResponse(
    Guid Id,
    string Title,
    string Genre,
    string Platform,
    string Status,
    string? CoverUrl,
    string? Description,
    decimal? Rating,
    int? ReleaseYear,
    DateTimeOffset AddedAt,
    DateTimeOffset UpdatedAt)
{
    public static GameResponse FromDomain(Game game) => new(
        game.Id,
        game.Title,
        game.Genre,
        game.Platform,
        game.Status.ToApiValue(),
        game.CoverUrl,
        game.Description,
        game.Rating,
        game.ReleaseYear,
        game.AddedAt,
        game.UpdatedAt);
}

/// <summary>Стандартный ответ со страницей данных.</summary>
public sealed record PagedResponse<T>(
    IReadOnlyList<T> Items,
    int Page,
    int PageSize,
    int TotalCount,
    int TotalPages,
    bool HasPrevious,
    bool HasNext);

/// <summary>Тело запроса на создание/полное обновление игры.</summary>
public sealed class SaveGameRequest
{
    [Required(ErrorMessage = "Название обязательно.")]
    [StringLength(200, MinimumLength = 2, ErrorMessage = "Название должно содержать от 2 до 200 символов.")]
    public string? Title { get; init; }

    [Required(ErrorMessage = "Жанр обязателен.")]
    [StringLength(100, MinimumLength = 1, ErrorMessage = "Жанр должен содержать от 1 до 100 символов.")]
    public string? Genre { get; init; }

    [Required(ErrorMessage = "Платформа обязательна.")]
    [StringLength(100, MinimumLength = 1, ErrorMessage = "Платформа должна содержать от 1 до 100 символов.")]
    public string? Platform { get; init; }

    [Required(ErrorMessage = "Статус обязателен.")]
    public string? Status { get; init; }

    [StringLength(500, ErrorMessage = "Ссылка на обложку не должна превышать 500 символов.")]
    public string? CoverUrl { get; init; }

    [StringLength(2000, ErrorMessage = "Описание не должно превышать 2000 символов.")]
    public string? Description { get; init; }

    [Range(0, 10, ErrorMessage = "Рейтинг должен быть от 0 до 10.")]
    public decimal? Rating { get; init; }

    [Range(1970, 2100, ErrorMessage = "Год выпуска должен быть от 1970 до 2100.")]
    public int? ReleaseYear { get; init; }
}

/// <summary>Тело запроса на смену статуса игры.</summary>
public sealed class ChangeGameStatusRequest
{
    [Required(ErrorMessage = "Статус обязателен.")]
    public string? Status { get; init; }
}

/// <summary>Профиль пользователя, возвращаемый клиенту.</summary>
public sealed record ProfileResponse(
    Guid Id,
    string Name,
    string Email,
    string? AvatarUrl,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt)
{
    public static ProfileResponse FromDomain(UserProfile profile) => new(
        profile.Id,
        profile.Name,
        profile.Email,
        profile.AvatarUrl,
        profile.CreatedAt,
        profile.UpdatedAt);
}

/// <summary>Тело запроса на обновление профиля.</summary>
public sealed class UpdateProfileRequest
{
    [Required(ErrorMessage = "Имя обязательно.")]
    [StringLength(100, MinimumLength = 2, ErrorMessage = "Имя должно содержать от 2 до 100 символов.")]
    public string? Name { get; init; }

    [Required(ErrorMessage = "Email обязателен.")]
    [EmailAddress(ErrorMessage = "Введите корректный email.")]
    [StringLength(254, ErrorMessage = "Email не должен превышать 254 символа.")]
    public string? Email { get; init; }

    [StringLength(500, ErrorMessage = "Ссылка на аватар не должна превышать 500 символов.")]
    public string? AvatarUrl { get; init; }
}

/// <summary>Тело запроса на вход.</summary>
public sealed class LoginRequest
{
    [Required(ErrorMessage = "Email обязателен.")]
    [EmailAddress(ErrorMessage = "Введите корректный email.")]
    public string? Email { get; init; }

    [Required(ErrorMessage = "Пароль обязателен.")]
    [StringLength(128, MinimumLength = 4, ErrorMessage = "Пароль должен содержать от 4 до 128 символов.")]
    public string? Password { get; init; }

    public bool Remember { get; init; }
}

/// <summary>Тело запроса на регистрацию нового аккаунта.</summary>
public sealed class RegisterRequest
{
    [Required(ErrorMessage = "Имя обязательно.")]
    [StringLength(100, MinimumLength = 2, ErrorMessage = "Имя должно содержать от 2 до 100 символов.")]
    public string? Name { get; init; }

    [Required(ErrorMessage = "Email обязателен.")]
    [EmailAddress(ErrorMessage = "Введите корректный email.")]
    [StringLength(254, ErrorMessage = "Email не должен превышать 254 символа.")]
    public string? Email { get; init; }

    [Required(ErrorMessage = "Пароль обязателен.")]
    [StringLength(128, MinimumLength = 8, ErrorMessage = "Пароль должен содержать от 8 до 128 символов.")]
    public string? Password { get; init; }

    public bool Remember { get; init; }
}

/// <summary>Сессия пользователя в ответе API.</summary>
public sealed record AuthSessionResponse(
    Guid UserId,
    string Email,
    string Name,
    DateTimeOffset IssuedAt,
    bool Remember)
{
    public static AuthSessionResponse FromDomain(AuthSession session) => new(
        session.UserId,
        session.Email,
        session.Name,
        session.IssuedAt,
        session.Remember);
}

/// <summary>Тело запроса на восстановление пароля.</summary>
public sealed class RecoveryRequest
{
    [Required(ErrorMessage = "Email обязателен.")]
    [EmailAddress(ErrorMessage = "Введите корректный email.")]
    public string? Email { get; init; }
}

/// <summary>Результат запроса восстановления пароля.</summary>
public sealed record RecoveryResponse(string Email, bool Delivered, string Code)
{
    public static RecoveryResponse FromDomain(RecoveryResult result) => new(
        result.Email,
        result.Delivered,
        result.Code);
}

/// <summary>Количество игр по статусу.</summary>
public sealed record StatusCountResponse(string Status, string Label, int Count);

public sealed record GenreCountResponse(string Genre, int Count);

public sealed record PlatformCountResponse(string Platform, int Count);

/// <summary>Сводная статистика библиотеки.</summary>
public sealed record StatisticsResponse(
    int Total,
    int Completed,
    int InProgress,
    int WantToPlay,
    int Dropped,
    int CompletionRate,
    decimal? AverageRating,
    IReadOnlyList<StatusCountResponse> ByStatus,
    IReadOnlyList<GenreCountResponse> ByGenre,
    IReadOnlyList<PlatformCountResponse> ByPlatform)
{
    private static readonly IReadOnlyDictionary<GameStatus, string> StatusLabels =
        new Dictionary<GameStatus, string>
        {
            [GameStatus.InProgress] = "В процессе",
            [GameStatus.Completed] = "Пройдено",
            [GameStatus.WantToPlay] = "Хочу пройти",
            [GameStatus.Dropped] = "Брошено"
        };

    public static StatisticsResponse FromDomain(LibraryStatistics statistics) => new(
        statistics.Total,
        statistics.Completed,
        statistics.InProgress,
        statistics.WantToPlay,
        statistics.Dropped,
        statistics.CompletionRate,
        statistics.AverageRating,
        statistics.ByStatus
            .Select(item => new StatusCountResponse(
                item.Status.ToApiValue(),
                StatusLabels[item.Status],
                item.Count))
            .ToList(),
        statistics.ByGenre
            .Select(item => new GenreCountResponse(item.Genre, item.Count))
            .ToList(),
        statistics.ByPlatform
            .Select(item => new PlatformCountResponse(item.Platform, item.Count))
            .ToList());
}

/// <summary>Единый формат ошибки API.</summary>
public sealed record ApiErrorResponse(string Message, string? TraceId = null);
