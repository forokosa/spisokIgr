using GameTrack.Domain.Common;
using GameTrack.Domain.Entities;
using GameTrack.Domain.Models;

namespace GameTrack.Domain.Repositories;

/// <summary>
/// Абстракция хранилища игр. Реализация живёт в Infrastructure,
/// все операции асинхронные, так как работа с БД — всегда I/O.
/// </summary>
public interface IGameRepository
{
    Task<PagedResult<Game>> GetPagedAsync(
        GameFilter filter,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Game>> GetAllAsync(CancellationToken cancellationToken = default);

    Task<Game?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<Game> AddAsync(Game game, CancellationToken cancellationToken = default);

    Task<Game> UpdateAsync(Game game, CancellationToken cancellationToken = default);

    Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default);

    Task<bool> ExistsByTitleAsync(
        string title,
        Guid? excludeId = null,
        CancellationToken cancellationToken = default);
}
