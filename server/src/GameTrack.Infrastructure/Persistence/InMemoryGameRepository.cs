using GameTrack.Domain.Common;
using GameTrack.Domain.Entities;
using GameTrack.Domain.Enums;
using GameTrack.Domain.Models;
using GameTrack.Domain.Repositories;

namespace GameTrack.Infrastructure.Persistence;

/// <summary>
/// In-memory заглушка репозитория игр. Вся работа с "хранилищем" инкапсулирована здесь.
/// Методы намеренно асинхронные: при переходе на реальную БД сигнатуры не изменятся.
/// </summary>
public sealed class InMemoryGameRepository : IGameRepository
{
    private readonly object _sync = new();
    private readonly List<Game> _games;

    public InMemoryGameRepository() : this(SeedData.CreateGames())
    {
    }

    public InMemoryGameRepository(IReadOnlyList<Game> seed)
    {
        _games = Guard.NotNull(seed, nameof(seed))
            .Select(Clone)
            .ToList();
    }

    public async Task<PagedResult<Game>> GetPagedAsync(
        GameFilter filter,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        Guard.NotNull(filter, nameof(filter));
        Guard.Page(page);
        Guard.PageSize(pageSize, int.MaxValue);
        await Task.CompletedTask;
        cancellationToken.ThrowIfCancellationRequested();

        lock (_sync)
        {
            var query = ApplyFilter(_games, filter)
                .OrderByDescending(game => game.UpdatedAt)
                .ThenBy(game => game.Title, StringComparer.OrdinalIgnoreCase)
                .ToList();

            var items = query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(Clone)
                .ToList();

            return new PagedResult<Game>(items, page, pageSize, query.Count);
        }
    }

    public async Task<IReadOnlyList<Game>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        await Task.CompletedTask;
        cancellationToken.ThrowIfCancellationRequested();

        lock (_sync)
        {
            return _games
                .OrderByDescending(game => game.UpdatedAt)
                .Select(Clone)
                .ToList();
        }
    }

    public async Task<Game?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        Guard.Id(id, nameof(id));
        await Task.CompletedTask;
        cancellationToken.ThrowIfCancellationRequested();

        lock (_sync)
        {
            var game = _games.FirstOrDefault(item => item.Id == id);
            return game is null ? null : Clone(game);
        }
    }

    public async Task<Game> AddAsync(Game game, CancellationToken cancellationToken = default)
    {
        Guard.NotNull(game, nameof(game));
        await Task.CompletedTask;
        cancellationToken.ThrowIfCancellationRequested();

        lock (_sync)
        {
            if (_games.Any(item => item.Id == game.Id))
            {
                throw new ConflictException("Игра с таким идентификатором уже существует.");
            }

            if (_games.Any(item => string.Equals(item.Title, game.Title, StringComparison.OrdinalIgnoreCase)))
            {
                throw new ConflictException("Игра с таким названием уже есть в библиотеке.");
            }

            var stored = Clone(game);
            _games.Add(stored);
            return Clone(stored);
        }
    }

    public async Task<Game> UpdateAsync(Game game, CancellationToken cancellationToken = default)
    {
        Guard.NotNull(game, nameof(game));
        await Task.CompletedTask;
        cancellationToken.ThrowIfCancellationRequested();

        lock (_sync)
        {
            var index = _games.FindIndex(item => item.Id == game.Id);
            if (index < 0)
            {
                throw new NotFoundException("Игра не найдена.");
            }

            if (_games.Any(item =>
                    item.Id != game.Id
                    && string.Equals(item.Title, game.Title, StringComparison.OrdinalIgnoreCase)))
            {
                throw new ConflictException("Игра с таким названием уже есть в библиотеке.");
            }

            var stored = Clone(game);
            _games[index] = stored;
            return Clone(stored);
        }
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        Guard.Id(id, nameof(id));
        await Task.CompletedTask;
        cancellationToken.ThrowIfCancellationRequested();

        lock (_sync)
        {
            return _games.RemoveAll(item => item.Id == id) > 0;
        }
    }

    public async Task<bool> ExistsByTitleAsync(
        string title,
        Guid? excludeId = null,
        CancellationToken cancellationToken = default)
    {
        var normalized = Guard.NotBlank(title, nameof(title), 2, 200);
        if (excludeId == Guid.Empty)
        {
            throw new ArgumentException("Исключаемый идентификатор не может быть пустым.", nameof(excludeId));
        }

        await Task.CompletedTask;
        cancellationToken.ThrowIfCancellationRequested();

        lock (_sync)
        {
            return _games.Any(game =>
                game.Id != excludeId
                && string.Equals(game.Title, normalized, StringComparison.OrdinalIgnoreCase));
        }
    }

    private static IEnumerable<Game> ApplyFilter(IEnumerable<Game> games, GameFilter filter)
    {
        var query = games;

        if (filter.Status is not null)
        {
            query = query.Where(game => game.Status == filter.Status);
        }

        var search = filter.Search?.Trim();
        if (!string.IsNullOrEmpty(search))
        {
            query = query.Where(game =>
                game.Title.Contains(search, StringComparison.OrdinalIgnoreCase)
                || game.Genre.Contains(search, StringComparison.OrdinalIgnoreCase)
                || game.Platform.Contains(search, StringComparison.OrdinalIgnoreCase));
        }

        return query;
    }

    private static Game Clone(Game game) => Game.Restore(
        game.Id,
        game.Title,
        game.Genre,
        game.Platform,
        game.Status,
        game.CoverUrl,
        game.Description,
        game.Rating,
        game.ReleaseYear,
        game.AddedAt,
        game.UpdatedAt);
}
