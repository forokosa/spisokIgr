using GameTrack.Domain.Enums;

namespace GameTrack.Infrastructure.Persistence.Models;

/// <summary>
/// Строка таблицы games. Это модель хранения (database first), а не доменная
/// сущность: у доменной <c>Game</c> Guid-идентификатор и строковые
/// жанр/платформа, здесь же повторяется реальная схема БД.
/// </summary>
public sealed class GameRecord
{
    public long Id { get; set; }

    public long AccountId { get; set; }

    public string Title { get; set; } = string.Empty;

    public string? CoverUrl { get; set; }

    public string? Description { get; set; }

    /// <summary>Внешний ключ games.status_id -> game_statuses.id (smallint).</summary>
    public short StatusId { get; set; }

    /// <summary>
    /// Статус из справочника. Связь с <see cref="StatusId"/>: именно по этой
    /// навигации EF идёт в game_statuses, поэтому значения enum сравниваются
    /// с game_statuses.code, а не с номером строки.
    /// </summary>
    public GameStatusRecord Status { get; set; } = null!;

    public decimal? Rating { get; set; }

    public decimal HoursPlayed { get; set; }

    public short? ReleaseYear { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public DateTimeOffset UpdatedAt { get; set; }

    public ICollection<GameGenreRecord> GameGenres { get; set; } = [];

    public ICollection<GamePlatformRecord> GamePlatforms { get; set; } = [];
}

/// <summary>Справочник game_statuses.</summary>
public sealed class GameStatusRecord
{
    public short Id { get; set; }

    /// <summary>Код статуса; мапится на enum <see cref="GameStatus"/>.</summary>
    public GameStatus Code { get; set; }

    public string Name { get; set; } = string.Empty;
}

/// <summary>Справочник genres.</summary>
public sealed class GenreRecord
{
    public long Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public ICollection<GameGenreRecord> GameGenres { get; set; } = [];
}

/// <summary>Справочник platforms.</summary>
public sealed class PlatformRecord
{
    public long Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public ICollection<GamePlatformRecord> GamePlatforms { get; set; } = [];
}

/// <summary>Связь игры с жанром (game_genres).</summary>
public sealed class GameGenreRecord
{
    public long GameId { get; set; }

    public long GenreId { get; set; }

    public GameRecord Game { get; set; } = null!;

    public GenreRecord Genre { get; set; } = null!;
}

/// <summary>Связь игры с платформой (game_platforms).</summary>
public sealed class GamePlatformRecord
{
    public long GameId { get; set; }

    public long PlatformId { get; set; }

    public GameRecord Game { get; set; } = null!;

    public PlatformRecord Platform { get; set; } = null!;
}
