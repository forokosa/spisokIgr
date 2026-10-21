using GameTrack.Domain.Enums;

namespace GameTrack.Domain.Models;

public sealed record StatusCount(GameStatus Status, int Count);

public sealed record GenreCount(string Genre, int Count);

public sealed record PlatformCount(string Platform, int Count);

/// <summary>Сводная статистика библиотеки, рассчитанная в Domain.</summary>
public sealed record LibraryStatistics(
    int Total,
    int Completed,
    int InProgress,
    int WantToPlay,
    int Dropped,
    int CompletionRate,
    decimal? AverageRating,
    IReadOnlyList<StatusCount> ByStatus,
    IReadOnlyList<GenreCount> ByGenre,
    IReadOnlyList<PlatformCount> ByPlatform);
