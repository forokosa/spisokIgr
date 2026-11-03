using GameTrack.Domain.Common;
using GameTrack.Domain.Models;
using GameTrack.Domain.Repositories;

namespace GameTrack.Domain.Services;

/// <summary>Расчёт сводной статистики библиотеки.</summary>
public sealed class StatisticsService
{
    private readonly IGameRepository _repository;
    private readonly IUserContextProvider _users;

    public StatisticsService(IGameRepository repository, IUserContextProvider users)
    {
        _repository = Guard.NotNull(repository, nameof(repository));
        _users = Guard.NotNull(users, nameof(users));
    }

    public async Task<LibraryStatistics> GetStatisticsAsync(CancellationToken cancellationToken = default)
    {
        var games = await _repository.GetAllAsync(_users.GetRequiredUser(), cancellationToken);
        return LibraryStatisticsCalculator.Calculate(games);
    }
}
