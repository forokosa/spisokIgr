using GameTrack.Domain.Common;
using GameTrack.Domain.Entities;
using GameTrack.Domain.Enums;
using GameTrack.Domain.Models;
using GameTrack.Domain.Repositories;

namespace GameTrack.Tests.Fakes;

/// <summary>
/// In-memory реализация библиотеки игр для быстрых unit-тестов домена,
/// контроллеров и сервисов. В рабочем коде её больше нет — источником
/// данных служит PostgreSQL.
///
/// Изоляция по пользователю здесь та же, что и у EF-репозитория: у каждого
/// <see cref="UserContext.UserId"/> своя коллекция игр.
/// </summary>
public sealed class FakeGameRepository : IGameRepository
{
    private readonly object _sync = new();
    private readonly Dictionary<Guid, List<Game>> _games = [];

    public FakeGameRepository()
    {
    }

    public FakeGameRepository(Guid userId, IReadOnlyList<Game> seed)
    {
        _games[userId] = seed.Select(Clone).ToList();
    }

    public async Task<PagedResult<Game>> GetPagedAsync(
        UserContext user,
        GameFilter filter,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        Guard.NotNull(user, nameof(user));
        Guard.NotNull(filter, nameof(filter));
        Guard.Page(page);
        Guard.PageSize(pageSize, int.MaxValue);
        await Task.CompletedTask;
        cancellationToken.ThrowIfCancellationRequested();

        lock (_sync)
        {
            var query = ApplyFilter(Owned(user), filter)
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

    public async Task<IReadOnlyList<Game>> GetAllAsync(
        UserContext user,
        CancellationToken cancellationToken = default)
    {
        Guard.NotNull(user, nameof(user));
        await Task.CompletedTask;
        cancellationToken.ThrowIfCancellationRequested();

        lock (_sync)
        {
            return Owned(user)
                .OrderByDescending(game => game.UpdatedAt)
                .Select(Clone)
                .ToList();
        }
    }

    public async Task<Game?> GetByIdAsync(
        UserContext user,
        Guid id,
        CancellationToken cancellationToken = default)
    {
        Guard.NotNull(user, nameof(user));
        Guard.Id(id, nameof(id));
        await Task.CompletedTask;
        cancellationToken.ThrowIfCancellationRequested();

        lock (_sync)
        {
            var game = Owned(user).FirstOrDefault(item => item.Id == id);
            return game is null ? null : Clone(game);
        }
    }

    public async Task<Game> AddAsync(
        UserContext user,
        Game game,
        CancellationToken cancellationToken = default)
    {
        Guard.NotNull(user, nameof(user));
        Guard.NotNull(game, nameof(game));
        await Task.CompletedTask;
        cancellationToken.ThrowIfCancellationRequested();

        lock (_sync)
        {
            var games = Owned(user).ToList();

            if (games.Any(item => item.Id == game.Id))
            {
                throw new ConflictException("Игра с таким идентификатором уже существует.");
            }

            if (games.Any(item => string.Equals(item.Title, game.Title, StringComparison.OrdinalIgnoreCase)))
            {
                throw new ConflictException("Игра с таким названием уже есть в библиотеке.");
            }

            games.Add(Clone(game));
            _games[user.UserId] = games;
            return Clone(game);
        }
    }

    public async Task<Game> UpdateAsync(
        UserContext user,
        Game game,
        CancellationToken cancellationToken = default)
    {
        Guard.NotNull(user, nameof(user));
        Guard.NotNull(game, nameof(game));
        await Task.CompletedTask;
        cancellationToken.ThrowIfCancellationRequested();

        lock (_sync)
        {
            var games = Owned(user).ToList();
            var index = games.FindIndex(item => item.Id == game.Id);
            if (index < 0)
            {
                throw new NotFoundException("Игра не найдена.");
            }

            if (games.Any(item =>
                    item.Id != game.Id
                    && string.Equals(item.Title, game.Title, StringComparison.OrdinalIgnoreCase)))
            {
                throw new ConflictException("Игра с таким названием уже есть в библиотеке.");
            }

            games[index] = Clone(game);
            _games[user.UserId] = games;
            return Clone(game);
        }
    }

    public async Task<bool> DeleteAsync(
        UserContext user,
        Guid id,
        CancellationToken cancellationToken = default)
    {
        Guard.NotNull(user, nameof(user));
        Guard.Id(id, nameof(id));
        await Task.CompletedTask;
        cancellationToken.ThrowIfCancellationRequested();

        lock (_sync)
        {
            var games = Owned(user).ToList();
            var removed = games.RemoveAll(item => item.Id == id);
            _games[user.UserId] = games;
            return removed > 0;
        }
    }

    public async Task<bool> ExistsByTitleAsync(
        UserContext user,
        string title,
        Guid? excludeId = null,
        CancellationToken cancellationToken = default)
    {
        Guard.NotNull(user, nameof(user));

        var normalized = Guard.NotBlank(title, nameof(title), 2, 200);
        if (excludeId == Guid.Empty)
        {
            throw new ArgumentException("Исключаемый идентификатор не может быть пустым.", nameof(excludeId));
        }

        await Task.CompletedTask;
        cancellationToken.ThrowIfCancellationRequested();

        lock (_sync)
        {
            return Owned(user).Any(game =>
                game.Id != excludeId
                && string.Equals(game.Title, normalized, StringComparison.OrdinalIgnoreCase));
        }
    }

    private IReadOnlyList<Game> Owned(UserContext user)
        => _games.TryGetValue(user.UserId, out var games) ? games : [];

    private static IEnumerable<Game> ApplyFilter(IEnumerable<Game> games, GameFilter filter)
    {
        var query = games;

        if (filter.Status is { } status)
        {
            query = query.Where(game => game.Status == status);
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

    internal static Game Clone(Game game) => Game.Restore(
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
