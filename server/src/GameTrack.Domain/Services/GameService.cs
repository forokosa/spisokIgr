using GameTrack.Domain.Common;
using GameTrack.Domain.Entities;
using GameTrack.Domain.Enums;
using GameTrack.Domain.Models;
using GameTrack.Domain.Repositories;

namespace GameTrack.Domain.Services;

/// <summary>
/// Доменный сервис библиотеки игр. Здесь находится вся бизнес-логика:
/// валидация пагинации, проверка уникальности названия и работа с агрегатом <see cref="Game"/>.
/// Каждая операция выполняется от имени <see cref="UserContext"/>,
/// поэтому чужая игра недоступна даже при знании её идентификатора.
/// </summary>
public sealed class GameService
{
    private const int MaxSearchLength = 100;

    private readonly IGameRepository _repository;
    private readonly IUserContextProvider _users;
    private readonly PaginationOptions _pagination;

    public GameService(
        IGameRepository repository,
        IUserContextProvider users,
        PaginationOptions pagination)
    {
        _repository = Guard.NotNull(repository, nameof(repository));
        _users = Guard.NotNull(users, nameof(users));
        _pagination = Guard.NotNull(pagination, nameof(pagination));
        _pagination.EnsureValid();
    }

    /// <summary>Возвращает страницу игр с поиском и фильтром по статусу.</summary>
    public async Task<PagedResult<Game>> GetGamesAsync(
        int page,
        int? pageSize = null,
        string? search = null,
        string? status = null,
        CancellationToken cancellationToken = default)
    {
        Guard.Page(page);
        var size = pageSize ?? _pagination.DefaultPageSize;
        Guard.PageSize(size, _pagination.MaxPageSize);

        var normalizedSearch = Guard.OptionalString(search, nameof(search), MaxSearchLength);
        GameStatus? parsedStatus = null;
        if (!string.IsNullOrWhiteSpace(status) && !status.Equals("all", StringComparison.OrdinalIgnoreCase))
        {
            parsedStatus = GameStatusExtensions.Parse(status);
        }

        var filter = new GameFilter(normalizedSearch, parsedStatus);
        return await _repository.GetPagedAsync(
            _users.GetRequiredUser(),
            filter,
            page,
            size,
            cancellationToken);
    }

    /// <summary>Возвращает игру по идентификатору.</summary>
    public async Task<Game> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        Guard.Id(id, nameof(id));
        return await _repository.GetByIdAsync(_users.GetRequiredUser(), id, cancellationToken)
               ?? throw new NotFoundException("Игра не найдена.");
    }

    /// <summary>Создаёт игру с проверкой уникальности названия.</summary>
    public async Task<Game> CreateAsync(GameDraft draft, CancellationToken cancellationToken = default)
    {
        Guard.NotNull(draft, nameof(draft));

        var user = _users.GetRequiredUser();
        await EnsureTitleIsUniqueAsync(user, draft.Title, null, cancellationToken);

        var game = Game.Create(draft);
        return await _repository.AddAsync(user, game, cancellationToken);
    }

    /// <summary>Полностью обновляет игру.</summary>
    public async Task<Game> UpdateAsync(
        Guid id,
        GameDraft draft,
        CancellationToken cancellationToken = default)
    {
        Guard.Id(id, nameof(id));
        Guard.NotNull(draft, nameof(draft));

        var user = _users.GetRequiredUser();
        var game = await GetByIdAsync(id, cancellationToken);
        await EnsureTitleIsUniqueAsync(user, draft.Title, id, cancellationToken);
        game.Update(draft);

        return await _repository.UpdateAsync(user, game, cancellationToken);
    }

    /// <summary>Меняет статус игры (частичное обновление).</summary>
    public async Task<Game> ChangeStatusAsync(
        Guid id,
        string status,
        CancellationToken cancellationToken = default)
    {
        Guard.Id(id, nameof(id));
        var parsed = GameStatusExtensions.Parse(status);
        var game = await GetByIdAsync(id, cancellationToken);
        game.ChangeStatus(parsed);

        return await _repository.UpdateAsync(_users.GetRequiredUser(), game, cancellationToken);
    }

    /// <summary>Удаляет игру из библиотеки.</summary>
    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        Guard.Id(id, nameof(id));

        var user = _users.GetRequiredUser();
        _ = await _repository.GetByIdAsync(user, id, cancellationToken)
            ?? throw new NotFoundException("Игра не найдена.");

        var deleted = await _repository.DeleteAsync(user, id, cancellationToken);
        if (!deleted)
        {
            throw new NotFoundException("Игра не найдена.");
        }
    }

    private async Task EnsureTitleIsUniqueAsync(
        UserContext user,
        string? title,
        Guid? excludeId,
        CancellationToken cancellationToken)
    {
        var normalized = Guard.NotBlank(title, nameof(title), 2, 200);
        var exists = await _repository.ExistsByTitleAsync(user, normalized, excludeId, cancellationToken);
        if (exists)
        {
            throw new ConflictException("Игра с таким названием уже есть в библиотеке.");
        }
    }
}
