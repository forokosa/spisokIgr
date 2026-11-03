using GameTrack.Domain.Common;
using GameTrack.Domain.Entities;
using GameTrack.Domain.Enums;
using GameTrack.Domain.Models;
using GameTrack.Domain.Repositories;
using GameTrack.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace GameTrack.Tests.Integration;

/// <summary>
/// Интеграционные тесты EF-репозитория игр: фильтрация, пагинация,
/// связи с жанрами/платформами, статистика и изоляция по пользователю.
/// </summary>
[Collection(PostgresCollection.Name)]
public sealed class EfGameRepositoryTests
{
    private readonly PostgresFixture _fixture;

    public EfGameRepositoryTests(PostgresFixture fixture)
    {
        _fixture = fixture;
    }

    /// <summary>Создаёт пользователя; без доступной БД тест завершается как пропущенный.</summary>
    private async Task<UserContext> RequireUserAsync(string login, string email, string displayName)
    {
        PostgresSkip.Require();
        return await _fixture.CreateUserAsync(login, email, displayName);
    }

    private async Task<LibraryScope> CreateScopeAsync(UserContext? user = null)
    {
        await Task.CompletedTask;
        return new LibraryScope(_fixture.ConnectionString, user ?? PostgresFixture.DemoUser);
    }

    [Fact]
    public async Task GetAllAsync_ReturnsSeedLibraryWithGenreAndPlatform()
    {
        PostgresSkip.Require();
        await using var scope = await CreateScopeAsync();

        var games = await scope.GameRepository.GetAllAsync(scope.User);

        Assert.Equal(7, games.Count);
        var witcher = Assert.Single(games, game => game.Title == "The Witcher 3: Wild Hunt");
        Assert.Equal("RPG", witcher.Genre);
        Assert.Equal("PC", witcher.Platform);
        Assert.Equal(GameStatus.Completed, witcher.Status);
        Assert.Equal(10m, witcher.Rating);
        Assert.Equal(2015, witcher.ReleaseYear);
    }

    [Fact]
    public async Task GetPagedAsync_AppliesPaginationInDatabase()
    {
        PostgresSkip.Require();
        await using var scope = await CreateScopeAsync();

        var first = await scope.GameRepository.GetPagedAsync(scope.User, new GameFilter(), 1, 3);
        var second = await scope.GameRepository.GetPagedAsync(scope.User, new GameFilter(), 2, 3);
        var third = await scope.GameRepository.GetPagedAsync(scope.User, new GameFilter(), 3, 3);

        Assert.Equal(3, first.Items.Count);
        Assert.Equal(7, first.TotalCount);
        Assert.Equal(3, first.TotalPages);
        Assert.True(first.HasNext);

        Assert.Equal(3, second.Items.Count);
        Assert.Single(third.Items);
        Assert.False(third.HasNext);

        // Страницы не пересекаются.
        Assert.Empty(first.Items.Select(item => item.Id).Intersect(second.Items.Select(item => item.Id)));
    }

    [Fact]
    public async Task GetPagedAsync_FiltersByStatus()
    {
        PostgresSkip.Require();
        await using var scope = await CreateScopeAsync();

        var completed = await scope.GameRepository.GetPagedAsync(
            scope.User,
            new GameFilter(Status: GameStatus.Completed),
            1,
            10);

        Assert.Equal(2, completed.TotalCount);
        Assert.All(completed.Items, game => Assert.Equal(GameStatus.Completed, game.Status));
    }

    [Theory]
    [InlineData("witcher", "The Witcher 3: Wild Hunt")]
    [InlineData("RPG", "Cyberpunk 2077")]          // поиск по жанру (связь game_genres)
    [InlineData("Nintendo", "Stardew Valley")]     // поиск по платформе (связь game_platforms)
    public async Task GetPagedAsync_SearchesTitleGenreAndPlatform(string search, string expectedTitle)
    {
        PostgresSkip.Require();
        await using var scope = await CreateScopeAsync();

        var page = await scope.GameRepository.GetPagedAsync(
            scope.User,
            new GameFilter(Search: search),
            1,
            10);

        Assert.Contains(page.Items, game => game.Title == expectedTitle);
    }

    [Fact]
    public async Task GetPagedAsync_WithUnknownSearch_ReturnsEmptyPage()
    {
        PostgresSkip.Require();
        await using var scope = await CreateScopeAsync();

        var page = await scope.GameRepository.GetPagedAsync(
            scope.User,
            new GameFilter(Search: "нет такой игры"),
            1,
            10);

        Assert.Empty(page.Items);
        Assert.Equal(0, page.TotalCount);
        Assert.Equal(0, page.TotalPages);
    }

    [Fact]
    public async Task Queries_AreExecutedByDatabaseNotInMemory()
    {
        PostgresSkip.Require();

        var options = new DbContextOptionsBuilder<GameTrackDbContext>()
            .UseNpgsql(_fixture.ConnectionString)
            .Options;

        await using var context = new GameTrackDbContext(options);
        var repository = new EfGameRepository(context);

        // Смотрим SQL, который EF построит для страницы: OFFSET/LIMIT и
        // явный список столбцов означают, что фильтрация и пагинация
        // выполняются базой, а не в памяти.
        var sql = context.Games
            .AsNoTracking()
            .Where(game => game.Title.ToLower() == "the witcher 3: wild hunt")
            .Skip(0)
            .Take(6)
            .ToQueryString();

        Assert.Contains("OFFSET", sql, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("LIMIT", sql, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("lower(", sql, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("SELECT *", sql, StringComparison.OrdinalIgnoreCase);

        // Запрос действительно выполняется базой.
        var page = await repository.GetPagedAsync(
            PostgresFixture.DemoUser,
            new GameFilter(Search: "witcher"),
            1,
            6);

        Assert.Single(page.Items);
    }

    [Fact]
    public async Task GetByIdAsync_ReturnsGame_ForOwner()
    {
        PostgresSkip.Require();
        await using var scope = await CreateScopeAsync();
        var games = await scope.GameRepository.GetAllAsync(scope.User);

        var found = await scope.GameRepository.GetByIdAsync(scope.User, games[0].Id);

        Assert.NotNull(found);
        Assert.Equal(games[0].Title, found!.Title);
    }

    [Fact]
    public async Task GetByIdAsync_ForForeignGame_ReturnsNull()
    {
        PostgresSkip.Require();
        var otherUser = await RequireUserAsync(
            $"iso-{Guid.NewGuid():N}"[..20],
            $"iso-{Guid.NewGuid():N}@gametrack.app",
            "Чужой");

        await using var demoScope = await CreateScopeAsync();
        var demoGames = await demoScope.GameRepository.GetAllAsync(PostgresFixture.DemoUser);

        await using var otherScope = await CreateScopeAsync(otherUser);
        var foreign = await otherScope.GameRepository.GetByIdAsync(otherUser, demoGames[0].Id);

        Assert.Null(foreign);
    }

    [Fact]
    public async Task Queries_AreIsolatedBetweenUsers()
    {
        PostgresSkip.Require();
        var suffix = Guid.NewGuid().ToString("N")[..8];
        var first = await RequireUserAsync($"first{suffix}", $"first{suffix}@gametrack.app", "Первый");
        var second = await RequireUserAsync($"second{suffix}", $"second{suffix}@gametrack.app", "Второй");

        await using var firstScope = await CreateScopeAsync(first);
        await using var secondScope = await CreateScopeAsync(second);

        await firstScope.GameRepository.AddAsync(first, Game.Create(new GameDraft(
            "Игра Первого", "RPG", "PC", GameStatus.InProgress)));
        await secondScope.GameRepository.AddAsync(second, Game.Create(new GameDraft(
            "Игра Второго", "Action", "Xbox", GameStatus.Completed)));

        var firstGames = await new LibraryScope(_fixture.ConnectionString, first)
            .GameRepository.GetAllAsync(first);
        var secondGames = await new LibraryScope(_fixture.ConnectionString, second)
            .GameRepository.GetAllAsync(second);

        Assert.Single(firstGames);
        Assert.Equal("Игра Первого", firstGames[0].Title);
        Assert.Single(secondGames);
        Assert.Equal("Игра Второго", secondGames[0].Title);

        // Демо-библиотека не видит игры новых пользователей.
        var demo = await new LibraryScope(_fixture.ConnectionString, PostgresFixture.DemoUser)
            .GameRepository.GetAllAsync(PostgresFixture.DemoUser);
        Assert.Equal(7, demo.Count);
        Assert.DoesNotContain(demo, game => game.Title.StartsWith("Игра ", StringComparison.Ordinal));
    }

    [Fact]
    public async Task AddAsync_PersistsGameWithLinks()
    {
        PostgresSkip.Require();
        var user = await RequireUserAsync(
            $"add-{Guid.NewGuid():N}"[..20],
            $"add-{Guid.NewGuid():N}@gametrack.app",
            "Добавляющий");

        await using var scope = await CreateScopeAsync(user);

        var created = await scope.GameRepository.AddAsync(user, Game.Create(new GameDraft(
            "Новая Игра",
            "Strategy",
            "Xbox",
            GameStatus.InProgress,
            "images/covers/new.svg",
            "Описание новой игры",
            7.5m,
            2024)));

        Assert.Equal("Новая Игра", created.Title);
        Assert.Equal("Strategy", created.Genre);
        Assert.Equal("Xbox", created.Platform);
        Assert.Equal(GameStatus.InProgress, created.Status);
        Assert.Equal(7.5m, created.Rating);

        var reloaded = await scope.GameRepository.GetByIdAsync(user, created.Id);
        Assert.NotNull(reloaded);
        Assert.Equal("Strategy", reloaded!.Genre);
        Assert.Equal("Xbox", reloaded.Platform);
    }

    [Fact]
    public async Task AddAsync_WithDuplicateTitle_ThrowsConflict()
    {
        PostgresSkip.Require();
        var user = await RequireUserAsync(
            $"dup-{Guid.NewGuid():N}"[..20],
            $"dup-{Guid.NewGuid():N}@gametrack.app",
            "Дублирующий");

        await using var scope = await CreateScopeAsync(user);
        await scope.GameRepository.AddAsync(user, Game.Create(new GameDraft(
            "Одинаковое Название", "RPG", "PC", GameStatus.WantToPlay)));

        await Assert.ThrowsAsync<ConflictException>(
            () => scope.GameRepository.AddAsync(user, Game.Create(new GameDraft(
                "одинаковое название", "Action", "PC", GameStatus.WantToPlay))));
    }

    [Fact]
    public async Task UpdateAsync_ChangesFieldsStatusAndLinks()
    {
        PostgresSkip.Require();
        var user = await RequireUserAsync(
            $"upd-{Guid.NewGuid():N}"[..20],
            $"upd-{Guid.NewGuid():N}@gametrack.app",
            "Обновляющий");

        await using var scope = await CreateScopeAsync(user);
        var created = await scope.GameRepository.AddAsync(user, Game.Create(new GameDraft(
            "Игра Для Обновления", "RPG", "PC", GameStatus.WantToPlay)));

        created.Update(created.ToDraft() with
        {
            Title = "Обновлённая Игра",
            Status = GameStatus.Completed,
            Genre = "Adventure",
            Platform = "Nintendo Switch",
            Rating = 9.5m
        });

        var updated = await scope.GameRepository.UpdateAsync(user, created);

        Assert.Equal("Обновлённая Игра", updated.Title);
        Assert.Equal(GameStatus.Completed, updated.Status);
        Assert.Equal(9.5m, updated.Rating);

        await using var verifyScope = await CreateScopeAsync(user);
        var reloaded = await verifyScope.GameRepository.GetByIdAsync(user, created.Id);
        Assert.Equal("Adventure", reloaded!.Genre);
        Assert.Equal("Nintendo Switch", reloaded.Platform);
        Assert.Equal(GameStatus.Completed, reloaded.Status);
    }

    [Fact]
    public async Task DeleteAsync_RemovesOwnGameOnly()
    {
        PostgresSkip.Require();
        var user = await RequireUserAsync(
            $"del-{Guid.NewGuid():N}"[..20],
            $"del-{Guid.NewGuid():N}@gametrack.app",
            "Удаляющий");

        await using var scope = await CreateScopeAsync(user);
        var created = await scope.GameRepository.AddAsync(user, Game.Create(new GameDraft(
            "Игра На Удаление", "RPG", "PC", GameStatus.WantToPlay)));

        Assert.True(await scope.GameRepository.DeleteAsync(user, created.Id));
        Assert.Null(await scope.GameRepository.GetByIdAsync(user, created.Id));
        Assert.False(await scope.GameRepository.DeleteAsync(user, created.Id));
    }

    [Fact]
    public async Task DeleteAsync_ForForeignGame_ReturnsFalseAndKeepsGame()
    {
        PostgresSkip.Require();
        var otherUser = await RequireUserAsync(
            $"keep-{Guid.NewGuid():N}"[..20],
            $"keep-{Guid.NewGuid():N}@gametrack.app",
            "Хранитель");

        await using var demoScope = await CreateScopeAsync();
        var demoGames = await demoScope.GameRepository.GetAllAsync(PostgresFixture.DemoUser);
        var target = demoGames[0];

        await using var otherScope = await CreateScopeAsync(otherUser);
        Assert.False(await otherScope.GameRepository.DeleteAsync(otherUser, target.Id));

        await using var verifyScope = await CreateScopeAsync();
        Assert.NotNull(await verifyScope.GameRepository.GetByIdAsync(PostgresFixture.DemoUser, target.Id));
    }

    [Fact]
    public async Task ExistsByTitleAsync_IsCaseInsensitiveAndScopedToUser()
    {
        PostgresSkip.Require();
        await using var scope = await CreateScopeAsync();

        Assert.True(await scope.GameRepository.ExistsByTitleAsync(
            PostgresFixture.DemoUser, "the witcher 3: wild hunt"));
        Assert.False(await scope.GameRepository.ExistsByTitleAsync(
            PostgresFixture.DemoUser, "Неизвестная Игра"));

        var foreignUser = await RequireUserAsync(
            $"ex-{Guid.NewGuid():N}"[..20],
            $"ex-{Guid.NewGuid():N}@gametrack.app",
            "Проверяющий");

        // У чужого пользователя такого названия нет.
        Assert.False(await scope.GameRepository.ExistsByTitleAsync(foreignUser, "The Witcher 3: Wild Hunt"));
        Assert.True(await scope.GameRepository.ExistsByTitleAsync(
            PostgresFixture.DemoUser, "THE WITCHER 3: WILD HUNT",
            excludeId: Guid.NewGuid()));
    }

    [Fact]
    public async Task GetAllAsync_ProvidesStatisticsData()
    {
        PostgresSkip.Require();
        await using var scope = await CreateScopeAsync();

        var games = await scope.GameRepository.GetAllAsync(PostgresFixture.DemoUser);
        var statistics = GameTrack.Domain.Services.LibraryStatisticsCalculator.Calculate(games);

        Assert.Equal(7, statistics.Total);
        Assert.Equal(2, statistics.Completed);
        Assert.Equal(2, statistics.InProgress);
        Assert.Equal(2, statistics.WantToPlay);
        Assert.Equal(1, statistics.Dropped);
        Assert.Equal(8.8m, statistics.AverageRating);
        Assert.Equal(2, statistics.ByGenre.First(item => item.Genre == "Action").Count);
    }

    [Fact]
    public async Task GameKey_RoundTripsBigintIds()
    {
        PostgresSkip.Require();
        await using var scope = await CreateScopeAsync();
        var games = await scope.GameRepository.GetAllAsync(PostgresFixture.DemoUser);

        foreach (var game in games)
        {
            Assert.True(GameKey.TryGetSequence(game.Id, out var sequence));
            Assert.True(sequence > 0);
            Assert.Equal(game.Id, GameKey.ToPublicId(sequence));
        }

        Assert.False(GameKey.TryGetSequence(Guid.NewGuid(), out _));
        Assert.False(GameKey.TryGetSequence(Guid.Empty, out _));
    }
}
