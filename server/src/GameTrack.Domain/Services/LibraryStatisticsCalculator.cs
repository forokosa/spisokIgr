using GameTrack.Domain.Common;
using GameTrack.Domain.Entities;
using GameTrack.Domain.Enums;
using GameTrack.Domain.Models;

namespace GameTrack.Domain.Services;

/// <summary>
/// Чистая доменная логика расчёта статистики. Выделена отдельно,
/// чтобы её можно было тестировать без репозитория.
/// </summary>
public static class LibraryStatisticsCalculator
{
    public static LibraryStatistics Calculate(IReadOnlyCollection<Game> games)
    {
        Guard.NotNull(games, nameof(games));

        var total = games.Count;
        var statusCounts = games
            .GroupBy(game => game.Status)
            .ToDictionary(group => group.Key, group => group.Count());

        int Count(GameStatus status) => statusCounts.GetValueOrDefault(status);

        var byStatus = GameStatusExtensions.All
            .Select(status => new StatusCount(status, Count(status)))
            .ToList();

        var byGenre = games
            .GroupBy(game => game.Genre, StringComparer.OrdinalIgnoreCase)
            .Select(group => new GenreCount(group.Key, group.Count()))
            .OrderByDescending(item => item.Count)
            .ThenBy(item => item.Genre, StringComparer.OrdinalIgnoreCase)
            .ToList();

        var byPlatform = games
            .GroupBy(game => game.Platform, StringComparer.OrdinalIgnoreCase)
            .Select(group => new PlatformCount(group.Key, group.Count()))
            .OrderByDescending(item => item.Count)
            .ThenBy(item => item.Platform, StringComparer.OrdinalIgnoreCase)
            .ToList();

        var rated = games.Where(game => game.Rating.HasValue).Select(game => game.Rating!.Value).ToList();
        decimal? averageRating = rated.Count == 0
            ? null
            : Math.Round(rated.Average(), 1, MidpointRounding.AwayFromZero);

        var completed = Count(GameStatus.Completed);
        var completionRate = total == 0 ? 0 : (int)Math.Round(completed * 100d / total);

        return new LibraryStatistics(
            total,
            completed,
            Count(GameStatus.InProgress),
            Count(GameStatus.WantToPlay),
            Count(GameStatus.Dropped),
            completionRate,
            averageRating,
            byStatus,
            byGenre,
            byPlatform);
    }
}
