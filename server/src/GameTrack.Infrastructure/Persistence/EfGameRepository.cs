using GameTrack.Domain.Common;
using GameTrack.Domain.Entities;
using GameTrack.Domain.Enums;
using GameTrack.Domain.Models;
using GameTrack.Domain.Repositories;
using GameTrack.Infrastructure.Persistence.Converters;
using GameTrack.Infrastructure.Persistence.Models;
using Microsoft.EntityFrameworkCore;

namespace GameTrack.Infrastructure.Persistence;

/// <summary>
/// Библиотека игр на Entity Framework Core (режим database first).
///
/// Правила, которые здесь соблюдаются:
///   * КАЖДЫЙ запрос ограничен account_id пользователя, включая выборку
///     по идентификатору: <see cref="GetByIdAsync"/> для чужой игры возвращает
///     <c>null</c>, а не данные;
///   * чтение — только AsNoTracking;
///   * фильтрация, сортировка и пагинация выполняются в SQL (Where/OrderBy/
///     Skip/Take), а не в памяти;
///   * Include не используется: название жанра и платформы проецируются
///     через Select;
///   * enum GameStatus ↔ game_statuses.code — конвертер <see cref="GameStatusConverter"/>,
///     связь с games.status_id идёт через внешний ключ;
///   * id игры в домене — Guid: он однозначно выводится из bigint-ключа
///     (<see cref="GameKey"/>), поэтому публичный контракт API не меняется.
///
/// Это замена InMemoryUserGameRepositoryProvider: пользователь передаётся
/// в каждый вызов, а не выбирается из общего словаря репозиториев.
/// </summary>
public sealed class EfGameRepository : IGameRepository
{
    private const int MaxTitleLength = 200;

    // Жанр/платформа в контракте — обязательные строки. Если у игры в БД
    // связь отсутствует (легаси-данные), подставляем явное значение.
    private const string UnknownGenre = "Не указан";
    private const string UnknownPlatform = "Не указана";

    private readonly GameTrackDbContext _context;

    public EfGameRepository(GameTrackDbContext context)
    {
        _context = Guard.NotNull(context, nameof(context));
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

        var query = ApplyFilter(OwnedGames(user), filter);

        var totalCount = await query.CountAsync(cancellationToken);
        if (totalCount == 0)
        {
            return new PagedResult<Game>([], page, pageSize, 0);
        }

        // Пагинация выполняется в SQL: Skip/Take -> OFFSET/LIMIT.
        var rows = await Project(query
                .OrderByDescending(game => game.UpdatedAt)
                .ThenBy(game => game.Title)
                .Skip((page - 1) * pageSize)
                .Take(pageSize))
            .ToListAsync(cancellationToken);

        return new PagedResult<Game>(rows.Select(ToGame).ToList(), page, pageSize, totalCount);
    }

    public async Task<IReadOnlyList<Game>> GetAllAsync(
        UserContext user,
        CancellationToken cancellationToken = default)
    {
        Guard.NotNull(user, nameof(user));

        var rows = await Project(OwnedGames(user).OrderByDescending(game => game.UpdatedAt))
            .ToListAsync(cancellationToken);

        return rows.Select(ToGame).ToList();
    }

    public async Task<Game?> GetByIdAsync(
        UserContext user,
        Guid id,
        CancellationToken cancellationToken = default)
    {
        Guard.NotNull(user, nameof(user));
        Guard.Id(id, nameof(id));

        if (!GameKey.TryGetSequence(id, out var sequence))
        {
            // Идентификатор не выведен из ключа БД: такой игры в базе быть не может.
            return null;
        }

        var row = await Project(OwnedGames(user).Where(game => game.Id == sequence))
            .FirstOrDefaultAsync(cancellationToken);

        return row is null ? null : ToGame(row);
    }

    public async Task<Game> AddAsync(
        UserContext user,
        Game game,
        CancellationToken cancellationToken = default)
    {
        Guard.NotNull(user, nameof(user));
        Guard.NotNull(game, nameof(game));

        if (await ExistsByTitleAsync(user, game.Title, null, cancellationToken))
        {
            throw new ConflictException("Игра с таким названием уже есть в библиотеке.");
        }

        var accountId = await ResolveAccountIdAsync(user, cancellationToken);
        var genreId = await ResolveGenreIdAsync(game.Genre, cancellationToken);
        var platformId = await ResolvePlatformIdAsync(game.Platform, cancellationToken);
        var statusId = await ResolveStatusIdAsync(game.Status, cancellationToken);

        var record = new GameRecord
        {
            AccountId = accountId,
            Title = game.Title,
            CoverUrl = game.CoverUrl,
            Description = game.Description,
            StatusId = statusId,
            Rating = game.Rating,
            HoursPlayed = 0m,
            ReleaseYear = (short?)game.ReleaseYear,
            CreatedAt = game.AddedAt.ToUniversalTime(),
            UpdatedAt = game.UpdatedAt.ToUniversalTime()
        };

        _context.Games.Add(record);
        _context.GameGenres.Add(new GameGenreRecord { Game = record, GenreId = genreId });
        _context.GamePlatforms.Add(new GamePlatformRecord { Game = record, PlatformId = platformId });

        await SaveChangesAsync(cancellationToken);

        return await GetByIdAsync(user, GameKey.ToPublicId(record.Id), cancellationToken)
               ?? throw new NotFoundException("Игра не найдена.");
    }

    public async Task<Game> UpdateAsync(
        UserContext user,
        Game game,
        CancellationToken cancellationToken = default)
    {
        Guard.NotNull(user, nameof(user));
        Guard.NotNull(game, nameof(game));

        if (!GameKey.TryGetSequence(game.Id, out var sequence))
        {
            throw new NotFoundException("Игра не найдена.");
        }

        if (await ExistsByTitleAsync(user, game.Title, game.Id, cancellationToken))
        {
            throw new ConflictException("Игра с таким названием уже есть в библиотеке.");
        }

        var accountId = await ResolveAccountIdAsync(user, cancellationToken);

        var record = await _context.Games
            .FirstOrDefaultAsync(
                row => row.AccountId == accountId && row.Id == sequence,
                cancellationToken)
            ?? throw new NotFoundException("Игра не найдена.");

        var genreId = await ResolveGenreIdAsync(game.Genre, cancellationToken);
        var platformId = await ResolvePlatformIdAsync(game.Platform, cancellationToken);
        var statusId = await ResolveStatusIdAsync(game.Status, cancellationToken);

        record.Title = game.Title;
        record.CoverUrl = game.CoverUrl;
        record.Description = game.Description;
        record.StatusId = statusId;
        record.Rating = game.Rating;
        record.ReleaseYear = (short?)game.ReleaseYear;
        record.UpdatedAt = game.UpdatedAt.ToUniversalTime();

        await SyncLinksAsync(record.Id, genreId, platformId, cancellationToken);
        await SaveChangesAsync(cancellationToken);

        return await GetByIdAsync(user, game.Id, cancellationToken)
               ?? throw new NotFoundException("Игра не найдена.");
    }

    public async Task<bool> DeleteAsync(
        UserContext user,
        Guid id,
        CancellationToken cancellationToken = default)
    {
        Guard.NotNull(user, nameof(user));
        Guard.Id(id, nameof(id));

        if (!GameKey.TryGetSequence(id, out var sequence))
        {
            return false;
        }

        var accountId = await ResolveAccountIdAsync(user, cancellationToken);

        // Удаление строго в рамках своей библиотеки. Связи game_genres /
        // game_platforms удаляются каскадом (ON DELETE CASCADE в схеме).
        var deleted = await _context.Games
            .Where(row => row.AccountId == accountId && row.Id == sequence)
            .ExecuteDeleteAsync(cancellationToken);

        return deleted > 0;
    }

    public async Task<bool> ExistsByTitleAsync(
        UserContext user,
        string title,
        Guid? excludeId = null,
        CancellationToken cancellationToken = default)
    {
        Guard.NotNull(user, nameof(user));

        var normalized = Guard.NotBlank(title, nameof(title), 2, MaxTitleLength);
        if (excludeId == Guid.Empty)
        {
            throw new ArgumentException("Исключаемый идентификатор не может быть пустым.", nameof(excludeId));
        }

        var accountId = await ResolveAccountIdAsync(user, cancellationToken);
        var lowered = normalized.ToLowerInvariant();

        var query = _context.Games
            .AsNoTracking()
            .Where(game => game.AccountId == accountId && game.Title.ToLower() == lowered);

        if (excludeId is { } excluded && GameKey.TryGetSequence(excluded, out var excludedSequence))
        {
            query = query.Where(game => game.Id != excludedSequence);
        }

        return await query.AnyAsync(cancellationToken);
    }

    /// <summary>Выборка игр только указанного пользователя — основа всех запросов.</summary>
    private IQueryable<GameRecord> OwnedGames(UserContext user)
    {
        var login = user.Login.ToLowerInvariant();

        var accountId = _context.Accounts
            .Where(account => account.Login.ToLower() == login)
            .Select(account => account.Id)
            .FirstOrDefault();

        return _context.Games.AsNoTracking().Where(game => game.AccountId == accountId);
    }

    /// <summary>Фильтр поиска: название, жанр или платформа (всё в SQL, ILIKE).</summary>
    private static IQueryable<GameRecord> ApplyFilter(IQueryable<GameRecord> query, GameFilter filter)
    {
        if (filter.Status is { } status)
        {
            query = query.Where(game => game.Status.Code == status);
        }

        var search = filter.Search?.Trim();
        if (!string.IsNullOrEmpty(search))
        {
            var pattern = $"%{search}%";
            query = query.Where(game =>
                EF.Functions.ILike(game.Title, pattern)
                || game.GameGenres.Any(link => EF.Functions.ILike(link.Genre.Name, pattern))
                || game.GamePlatforms.Any(link => EF.Functions.ILike(link.Platform.Name, pattern)));
        }

        return query;
    }

    /// <summary>
    /// Проекция нужных полей: подзапросы вместо Include. «Главный» жанр и
    /// платформа — с минимальным id (контракт API отдаёт по одной строке).
    /// </summary>
    private static IQueryable<GameRow> Project(IQueryable<GameRecord> query) =>
        query.Select(game => new GameRow
        {
            Id = game.Id,
            Title = game.Title,
            Status = game.Status.Code,
            CoverUrl = game.CoverUrl,
            Description = game.Description,
            Rating = game.Rating,
            ReleaseYear = game.ReleaseYear,
            CreatedAt = game.CreatedAt,
            UpdatedAt = game.UpdatedAt,
            GenreName = game.GameGenres
                .OrderBy(link => link.GenreId)
                .Select(link => link.Genre.Name)
                .FirstOrDefault(),
            PlatformName = game.GamePlatforms
                .OrderBy(link => link.PlatformId)
                .Select(link => link.Platform.Name)
                .FirstOrDefault()
        });

    private static Game ToGame(GameRow row) => Game.Restore(
        GameKey.ToPublicId(row.Id),
        row.Title,
        row.GenreName ?? UnknownGenre,
        row.PlatformName ?? UnknownPlatform,
        row.Status,
        row.CoverUrl,
        row.Description,
        row.Rating,
        row.ReleaseYear,
        row.CreatedAt,
        row.UpdatedAt);

    private async Task<long> ResolveAccountIdAsync(UserContext user, CancellationToken cancellationToken)
    {
        var login = user.Login.ToLowerInvariant();

        var accountId = await _context.Accounts
            .AsNoTracking()
            .Where(account => account.Login.ToLower() == login)
            .Select(account => (long?)account.Id)
            .FirstOrDefaultAsync(cancellationToken);

        return accountId
               ?? throw new NotFoundException("Аккаунт текущего пользователя не найден.");
    }

    /// <summary>
    /// Жанр по названию. Незнакомый жанр заводится в справочнике: клиент может
    /// прислать собственный, а справочник — часть той же БД.
    /// </summary>
    private async Task<long> ResolveGenreIdAsync(string name, CancellationToken cancellationToken)
    {
        var normalized = Guard.NotBlank(name, nameof(name), 1, 50);
        var lowered = normalized.ToLowerInvariant();

        var existing = await _context.Genres
            .AsNoTracking()
            .Where(genre => genre.Name.ToLower() == lowered)
            .Select(genre => (long?)genre.Id)
            .FirstOrDefaultAsync(cancellationToken);

        if (existing is { } id)
        {
            return id;
        }

        var genre = new GenreRecord { Name = normalized };
        _context.Genres.Add(genre);
        await SaveChangesAsync(cancellationToken);
        return genre.Id;
    }

    /// <summary>
    /// Ключ game_statuses для доменного статуса. Ищем по коду справочника:
    /// собственно код и есть значение enum (Playing/Completed/Planned/Dropped),
    /// поэтому нумерация строк справочника в конкретной БД роли не играет.
    /// </summary>
    private async Task<short> ResolveStatusIdAsync(GameStatus status, CancellationToken cancellationToken)
    {
        var code = GameStatusConverter.ToCode(status);

        var statusId = await _context.GameStatuses
            .AsNoTracking()
            .Where(row => row.Code == status)
            .Select(row => (short?)row.Id)
            .FirstOrDefaultAsync(cancellationToken);

        return statusId
               ?? throw new InvalidOperationException(
                   $"В справочнике game_statuses нет статуса '{code}'. " +
                   "Выполните db/002_seed_reference_data.sql.");
    }

    /// <summary>Платформа по названию; незнакомая добавляется в справочник.</summary>
    private async Task<long> ResolvePlatformIdAsync(string name, CancellationToken cancellationToken)
    {
        var normalized = Guard.NotBlank(name, nameof(name), 1, 50);
        var lowered = normalized.ToLowerInvariant();

        var existing = await _context.Platforms
            .AsNoTracking()
            .Where(platform => platform.Name.ToLower() == lowered)
            .Select(platform => (long?)platform.Id)
            .FirstOrDefaultAsync(cancellationToken);

        if (existing is { } id)
        {
            return id;
        }

        var platform = new PlatformRecord { Name = normalized };
        _context.Platforms.Add(platform);
        await SaveChangesAsync(cancellationToken);
        return platform.Id;
    }

    /// <summary>Приводит связи игры к одному выбранному жанру и платформе.</summary>
    private async Task SyncLinksAsync(
        long gameId,
        long genreId,
        long platformId,
        CancellationToken cancellationToken)
    {
        await _context.GameGenres
            .Where(link => link.GameId == gameId && link.GenreId != genreId)
            .ExecuteDeleteAsync(cancellationToken);

        await _context.GamePlatforms
            .Where(link => link.GameId == gameId && link.PlatformId != platformId)
            .ExecuteDeleteAsync(cancellationToken);

        var hasGenre = await _context.GameGenres.AnyAsync(
            link => link.GameId == gameId && link.GenreId == genreId, cancellationToken);
        if (!hasGenre)
        {
            _context.GameGenres.Add(new GameGenreRecord { GameId = gameId, GenreId = genreId });
        }

        var hasPlatform = await _context.GamePlatforms.AnyAsync(
            link => link.GameId == gameId && link.PlatformId == platformId, cancellationToken);
        if (!hasPlatform)
        {
            _context.GamePlatforms.Add(new GamePlatformRecord { GameId = gameId, PlatformId = platformId });
        }
    }

    private async Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        try
        {
            await _context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException exception) when (IsUniqueViolation(exception))
        {
            throw new ConflictException("Игра с таким названием уже есть в библиотеке.");
        }
    }

    private static bool IsUniqueViolation(DbUpdateException exception)
        => exception.InnerException is Npgsql.PostgresException
        {
            SqlState: Npgsql.PostgresErrorCodes.UniqueViolation
        };

    /// <summary>Плоская строка результата проекции (только нужные поля).</summary>
    private sealed class GameRow
    {
        public long Id { get; init; }

        public string Title { get; init; } = string.Empty;

        public GameStatus Status { get; init; }

        public string? CoverUrl { get; init; }

        public string? Description { get; init; }

        public decimal? Rating { get; init; }

        public short? ReleaseYear { get; init; }

        public DateTimeOffset CreatedAt { get; init; }

        public DateTimeOffset UpdatedAt { get; init; }

        public string? GenreName { get; init; }

        public string? PlatformName { get; init; }
    }
}
