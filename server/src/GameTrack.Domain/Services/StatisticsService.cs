using GameTrack.Domain.Common;
using GameTrack.Domain.Models;
using GameTrack.Domain.Repositories;

namespace GameTrack.Domain.Services;

/// <summary>Расчёт сводной статистики библиотеки.</summary>
public sealed class StatisticsService
{
    private readonly IGameRepository _repository;

    public StatisticsService(IGameRepository repository)
    {
        _repository = Guard.NotNull(repository, nameof(repository));
    }

    public async Task<LibraryStatistics> GetStatisticsAsync(CancellationToken cancellationToken = default)
    {
        var games = await _repository.GetAllAsync(cancellationToken);
        return LibraryStatisticsCalculator.Calculate(games);
    }
}
