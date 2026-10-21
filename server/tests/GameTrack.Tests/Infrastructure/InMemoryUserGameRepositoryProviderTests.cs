using GameTrack.Domain.Entities;
using GameTrack.Domain.Enums;
using GameTrack.Domain.Models;
using GameTrack.Infrastructure.Persistence;

namespace GameTrack.Tests.Infrastructure;

public sealed class InMemoryUserGameRepositoryProviderTests
{
    private static readonly Guid DemoUserId = SeedData.ProfileId;

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
    public async Task GetRepository_ForDemoUser_ReturnsSeededLibrary()
    {
        var provider = new InMemoryUserGameRepositoryProvider(DemoUserId);

        var games = await provider.GetRepository(DemoUserId).GetAllAsync();

        Assert.Equal(7, games.Count);
    }

    [Fact]
    public async Task GetRepository_ForNewUser_ReturnsEmptyLibrary()
    {
        var provider = new InMemoryUserGameRepositoryProvider(DemoUserId);

        var games = await provider.GetRepository(Guid.NewGuid()).GetAllAsync();

        Assert.Empty(games);
    }

    [Fact]
    public async Task GetRepository_ForDifferentUsers_KeepsLibrariesIsolated()
    {
        var provider = new InMemoryUserGameRepositoryProvider(DemoUserId);
        var firstUser = Guid.NewGuid();
        var secondUser = Guid.NewGuid();

        await provider.GetRepository(firstUser).AddAsync(NewGame());
        await provider.GetRepository(secondUser).AddAsync(NewGame("Another Game"));

        Assert.Single(await provider.GetRepository(firstUser).GetAllAsync());
        Assert.Single(await provider.GetRepository(secondUser).GetAllAsync());
        Assert.Equal(7, (await provider.GetRepository(DemoUserId).GetAllAsync()).Count);
    }

    [Fact]
    public async Task GetRepository_WithSameUser_ReturnsSameRepository()
    {
        var provider = new InMemoryUserGameRepositoryProvider(DemoUserId);
        var userId = Guid.NewGuid();

        var first = provider.GetRepository(userId);
        await first.AddAsync(NewGame());
        var second = provider.GetRepository(userId);

        Assert.Single(await second.GetAllAsync());
    }

    [Fact]
    public async Task Constructor_WithEmptyDemoUserId_ThrowsArgumentException()
    {
        await Assert.ThrowsAsync<ArgumentException>(
            () => Task.FromResult(new InMemoryUserGameRepositoryProvider(Guid.Empty)));
    }
}
