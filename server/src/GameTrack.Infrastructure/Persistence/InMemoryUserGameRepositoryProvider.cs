using GameTrack.Domain.Common;
using GameTrack.Domain.Entities;
using GameTrack.Domain.Repositories;

namespace GameTrack.Infrastructure.Persistence;

/// <summary>
/// Хранит отдельную in-memory библиотеку для каждого аккаунта.
/// Демонстрационный пользователь получает стартовый набор игр,
/// а все новые аккаунты начинают с пустой библиотеки.
/// </summary>
public sealed class InMemoryUserGameRepositoryProvider
{
    private readonly object _sync = new();
    private readonly Dictionary<Guid, InMemoryGameRepository> _repositories = [];
    private readonly Guid _demoUserId;

    public InMemoryUserGameRepositoryProvider(Guid demoUserId)
    {
        _demoUserId = Guard.Id(demoUserId, nameof(demoUserId));
    }

    /// <summary>Возвращает репозиторий игр конкретного пользователя.</summary>
    public IGameRepository GetRepository(Guid userId)
    {
        var ownerId = Guard.Id(userId, nameof(userId));

        lock (_sync)
        {
            if (!_repositories.TryGetValue(ownerId, out var repository))
            {
                IReadOnlyList<Game> seed = ownerId == _demoUserId
                    ? SeedData.CreateGames()
                    : Array.Empty<Game>();

                repository = new InMemoryGameRepository(seed);
                _repositories[ownerId] = repository;
            }

            return repository;
        }
    }
}
