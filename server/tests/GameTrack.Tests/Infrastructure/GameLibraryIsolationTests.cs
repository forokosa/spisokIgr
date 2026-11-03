using GameTrack.Domain.Entities;
using GameTrack.Domain.Enums;
using GameTrack.Domain.Models;
using GameTrack.Domain.Repositories;
using GameTrack.Tests.Fakes;

namespace GameTrack.Tests.Infrastructure;

/// <summary>
/// Проверка изоляции библиотек по пользователю на in-memory реализации.
/// Раньше её обеспечивал отдельный InMemoryUserGameRepositoryProvider
/// (словарь репозиториев на аккаунт), теперь — <see cref="UserContext"/>
/// в каждом вызове репозитория. Интеграционные проверки того же правила
/// на реальной БД находятся в EfGameRepositoryIntegrationTests.
/// </summary>
public sealed class GameLibraryIsolationTests
{
    private static readonly UserContext DemoUser = new(TestSeed.ProfileId, TestSeed.Login);

    private static Game NewGame(string title = "Personal Game") => Game.Create(new GameDraft(
        title,
        "RPG",
        "PC",
        GameStatus.WantToPlay,
        null,
        "Описание",
        8m,
        2024));

    [Fact]
    public async Task DemoUser_HasSeededLibrary()
    {
        var repository = new FakeGameRepository(DemoUser.UserId, TestSeed.CreateGames());

        Assert.Equal(7, (await repository.GetAllAsync(DemoUser)).Count);
    }

    [Fact]
    public async Task NewUser_StartsWithEmptyLibrary()
    {
        var repository = new FakeGameRepository(DemoUser.UserId, TestSeed.CreateGames());
        var newUser = new UserContext(Guid.NewGuid(), "newbie");

        Assert.Empty(await repository.GetAllAsync(newUser));
    }

    [Fact]
    public async Task DifferentUsers_KeepLibrariesIsolated()
    {
        var repository = new FakeGameRepository(DemoUser.UserId, TestSeed.CreateGames());
        var firstUser = new UserContext(Guid.NewGuid(), "first");
        var secondUser = new UserContext(Guid.NewGuid(), "second");

        await repository.AddAsync(firstUser, NewGame());
        await repository.AddAsync(secondUser, NewGame("Another Game"));

        Assert.Single(await repository.GetAllAsync(firstUser));
        Assert.Single(await repository.GetAllAsync(secondUser));
        Assert.Equal(7, (await repository.GetAllAsync(DemoUser)).Count);
    }

    [Fact]
    public async Task GetByIdAsync_ForForeignGame_ReturnsNull()
    {
        var repository = new FakeGameRepository(DemoUser.UserId, TestSeed.CreateGames());
        var foreignUser = new UserContext(Guid.NewGuid(), "foreign");
        var demoGame = (await repository.GetAllAsync(DemoUser))[0];

        Assert.NotNull(await repository.GetByIdAsync(DemoUser, demoGame.Id));
        Assert.Null(await repository.GetByIdAsync(foreignUser, demoGame.Id));
    }

    [Fact]
    public async Task DeleteAsync_ForForeignGame_ReturnsFalse()
    {
        var repository = new FakeGameRepository(DemoUser.UserId, TestSeed.CreateGames());
        var foreignUser = new UserContext(Guid.NewGuid(), "foreign");
        var demoGame = (await repository.GetAllAsync(DemoUser))[0];

        Assert.False(await repository.DeleteAsync(foreignUser, demoGame.Id));
        Assert.NotNull(await repository.GetByIdAsync(DemoUser, demoGame.Id));
    }

    [Fact]
    public async Task GetPagedAsync_ForForeignUser_ReturnsEmptyPage()
    {
        var repository = new FakeGameRepository(DemoUser.UserId, TestSeed.CreateGames());
        var foreignUser = new UserContext(Guid.NewGuid(), "foreign");

        var page = await repository.GetPagedAsync(foreignUser, new GameFilter(), 1, 10);

        Assert.Empty(page.Items);
        Assert.Equal(0, page.TotalCount);
    }
}
