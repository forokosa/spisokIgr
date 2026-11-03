using GameTrack.Domain.Common;
using GameTrack.Domain.Enums;
using GameTrack.Domain.Models;
using GameTrack.Domain.Repositories;
using GameTrack.Domain.Services;
using GameTrack.Tests.Fakes;

namespace GameTrack.Tests.Domain;

public sealed class GameServiceTests
{
    private static readonly UserContext DemoUser = new(TestSeed.ProfileId, TestSeed.Login);

    private static GameService CreateService(
        out FakeGameRepository repository,
        out FakeUserContextProvider users,
        UserContext? user = null)
    {
        var demo = user ?? DemoUser;
        users = new FakeUserContextProvider(demo);
        repository = new FakeGameRepository(demo.UserId, TestSeed.CreateGames());
        return new GameService(repository, users, new PaginationOptions());
    }

    private static GameDraft Draft(string title = "New Game") => new(
        title,
        "RPG",
        "PC",
        GameStatus.WantToPlay,
        null,
        "Описание",
        8m,
        2021);

    [Fact]
    public async Task Constructor_WithNullRepository_ThrowsArgumentNullException()
    {
        await Assert.ThrowsAsync<ArgumentNullException>(
            () => Task.FromResult(new GameService(null!, new FakeUserContextProvider(DemoUser), new PaginationOptions())));
    }

    [Fact]
    public async Task Constructor_WithNullPagination_ThrowsArgumentNullException()
    {
        var repository = new FakeGameRepository(DemoUser.UserId, TestSeed.CreateGames());

        await Assert.ThrowsAsync<ArgumentNullException>(
            () => Task.FromResult(new GameService(repository, new FakeUserContextProvider(DemoUser), null!)));
    }

    [Fact]
    public async Task GetGamesAsync_ReturnsFirstPage()
    {
        var service = CreateService(out _, out _);

        var result = await service.GetGamesAsync(1, 3);

        Assert.Equal(3, result.Items.Count);
        Assert.Equal(7, result.TotalCount);
        Assert.Equal(3, result.TotalPages);
        Assert.True(result.HasNext);
        Assert.False(result.HasPrevious);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task GetGamesAsync_WithInvalidPage_ThrowsArgumentOutOfRange(int page)
    {
        var service = CreateService(out _, out _);

        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(
            () => service.GetGamesAsync(page, 10));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-5)]
    [InlineData(101)]
    public async Task GetGamesAsync_WithInvalidPageSize_ThrowsArgumentOutOfRange(int pageSize)
    {
        var service = CreateService(out _, out _);

        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(
            () => service.GetGamesAsync(1, pageSize));
    }

    [Fact]
    public async Task GetGamesAsync_WithTooLongSearch_ThrowsArgumentOutOfRange()
    {
        var service = CreateService(out _, out _);

        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(
            () => service.GetGamesAsync(1, 10, new string('x', 101)));
    }

    [Fact]
    public async Task GetGamesAsync_WithInvalidStatus_ThrowsArgumentException()
    {
        var service = CreateService(out _, out _);

        await Assert.ThrowsAsync<ArgumentException>(
            () => service.GetGamesAsync(1, 10, status: "broken"));
    }

    [Fact]
    public async Task GetGamesAsync_FiltersBySearchAndStatus()
    {
        var service = CreateService(out _, out _);

        var bySearch = await service.GetGamesAsync(1, 10, search: "witcher");
        var byStatus = await service.GetGamesAsync(1, 10, status: "want-to-play");

        Assert.Single(bySearch.Items);
        Assert.Equal("The Witcher 3: Wild Hunt", bySearch.Items[0].Title);
        Assert.Equal(2, byStatus.TotalCount);
        Assert.All(byStatus.Items, game => Assert.Equal(GameStatus.WantToPlay, game.Status));
    }

    [Fact]
    public async Task CreateAsync_WithValidDraft_AddsGame()
    {
        var service = CreateService(out var repository, out _);

        var game = await service.CreateAsync(Draft());

        Assert.Equal("New Game", game.Title);
        Assert.NotNull(await repository.GetByIdAsync(DemoUser, game.Id));
    }

    [Fact]
    public async Task CreateAsync_WithNullDraft_ThrowsArgumentNullException()
    {
        var service = CreateService(out _, out _);

        await Assert.ThrowsAsync<ArgumentNullException>(() => service.CreateAsync(null!));
    }

    [Fact]
    public async Task CreateAsync_WithDuplicateTitle_ThrowsConflict()
    {
        var service = CreateService(out _, out _);

        await Assert.ThrowsAsync<ConflictException>(
            () => service.CreateAsync(Draft("The Witcher 3: Wild Hunt")));
    }

    [Fact]
    public async Task GetByIdAsync_WithUnknownId_ThrowsNotFound()
    {
        var service = CreateService(out _, out _);

        await Assert.ThrowsAsync<NotFoundException>(() => service.GetByIdAsync(Guid.NewGuid()));
    }

    [Fact]
    public async Task GetByIdAsync_WithEmptyId_ThrowsArgumentException()
    {
        var service = CreateService(out _, out _);

        await Assert.ThrowsAsync<ArgumentException>(() => service.GetByIdAsync(Guid.Empty));
    }

    [Fact]
    public async Task GetByIdAsync_ForForeignUser_ThrowsNotFound()
    {
        var service = CreateService(out var repository, out var users);

        var foreignId = Guid.Parse("11111111-1111-4111-8111-111111111111");
        Assert.NotNull(await repository.GetByIdAsync(DemoUser, foreignId));

        // Тот же идентификатор игры, но другой пользователь — библиотека пуста.
        users.User = new UserContext(Guid.NewGuid(), "someone");

        await Assert.ThrowsAsync<NotFoundException>(() => service.GetByIdAsync(foreignId));
    }

    [Fact]
    public async Task UpdateAsync_WithValidDraft_UpdatesGame()
    {
        var service = CreateService(out _, out _);
        var games = await service.GetGamesAsync(1, 100);
        var target = games.Items[0];

        var updated = await service.UpdateAsync(
            target.Id,
            target.ToDraft() with { Title = "Renamed Game", Status = GameStatus.Completed });

        Assert.Equal("Renamed Game", updated.Title);
        Assert.Equal(GameStatus.Completed, updated.Status);
    }

    [Fact]
    public async Task UpdateAsync_WithUnknownId_ThrowsNotFound()
    {
        var service = CreateService(out _, out _);

        await Assert.ThrowsAsync<NotFoundException>(
            () => service.UpdateAsync(Guid.NewGuid(), Draft()));
    }

    [Fact]
    public async Task ChangeStatusAsync_WithValidStatus_UpdatesGame()
    {
        var service = CreateService(out _, out _);
        var games = await service.GetGamesAsync(1, 100);

        var updated = await service.ChangeStatusAsync(games.Items[0].Id, "dropped");

        Assert.Equal(GameStatus.Dropped, updated.Status);
    }

    [Fact]
    public async Task ChangeStatusAsync_WithInvalidStatus_ThrowsArgumentException()
    {
        var service = CreateService(out _, out _);
        var games = await service.GetGamesAsync(1, 100);

        await Assert.ThrowsAsync<ArgumentException>(
            () => service.ChangeStatusAsync(games.Items[0].Id, "unknown"));
    }

    [Fact]
    public async Task DeleteAsync_RemovesGame()
    {
        var service = CreateService(out var repository, out _);
        var games = await service.GetGamesAsync(1, 100);
        var id = games.Items[0].Id;

        await service.DeleteAsync(id);

        Assert.Null(await repository.GetByIdAsync(DemoUser, id));
    }

    [Fact]
    public async Task DeleteAsync_WithUnknownId_ThrowsNotFound()
    {
        var service = CreateService(out _, out _);

        await Assert.ThrowsAsync<NotFoundException>(() => service.DeleteAsync(Guid.NewGuid()));
    }

    [Fact]
    public async Task Pagination_SecondPage_ReturnsRemainingItems()
    {
        var service = CreateService(out _, out _);

        var first = await service.GetGamesAsync(1, 6);
        var second = await service.GetGamesAsync(2, 6);

        Assert.Equal(6, first.Items.Count);
        Assert.Single(second.Items);
        Assert.True(second.HasPrevious);
        Assert.False(second.HasNext);
    }
}
