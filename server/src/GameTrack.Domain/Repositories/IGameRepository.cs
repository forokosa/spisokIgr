using GameTrack.Domain.Common;
using GameTrack.Domain.Entities;
using GameTrack.Domain.Models;

namespace GameTrack.Domain.Repositories;

/// <summary>
/// Библиотека игр. Каждый метод получает <see cref="UserContext"/> и обязан
/// ограничивать операцию этим аккаунтом: например, <see cref="GetByIdAsync"/>
/// для чужой игры должен вернуть <c>null</c> (HTTP 404), а не саму игру.
/// </summary>
public interface IGameRepository
{
    Task<PagedResult<Game>> GetPagedAsync(
        UserContext user,
        GameFilter filter,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Game>> GetAllAsync(
        UserContext user,
        CancellationToken cancellationToken = default);

    Task<Game?> GetByIdAsync(
        UserContext user,
        Guid id,
        CancellationToken cancellationToken = default);

    Task<Game> AddAsync(
        UserContext user,
        Game game,
        CancellationToken cancellationToken = default);

    Task<Game> UpdateAsync(
        UserContext user,
        Game game,
        CancellationToken cancellationToken = default);

    Task<bool> DeleteAsync(
        UserContext user,
        Guid id,
        CancellationToken cancellationToken = default);

    Task<bool> ExistsByTitleAsync(
        UserContext user,
        string title,
        Guid? excludeId = null,
        CancellationToken cancellationToken = default);
}
