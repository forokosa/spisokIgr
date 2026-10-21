using GameTrack.Domain.Entities;
using GameTrack.Domain.Enums;
using GameTrack.Domain.Models;
using GameTrack.Infrastructure.Persistence;

namespace GameTrack.Tests.Infrastructure;

public sealed class InMemoryGameRepositoryTests
{
    private static InMemoryGameRepository CreateRepository() => new();

    private static Game NewGame(string title = "Brand New Game") => Game.Create(new GameDraft(
        title,
        "RPG",
        "PC",
        GameStatus.WantToPlay,
        "images/covers/new.svg",
        "Описание",
        7.5m,
        2022));

    [Fact]
    public async Task GetAllAsync_ReturnsSeededGames()
    {
        var repository = CreateRepository();

        var games = await repository.GetAllAsync();

        Assert.Equal(7, games.Count);
    }

    [Fact]
    public async Task GetAllAsync_ReturnsIsolatedCopies()
    {
        var repository = CreateRepository();

        var firstRead = await repository.GetAllAsync();
        firstRead[0].Update(firstRead[0].ToDraft() with { Title = "Mutated" });
        var secondRead = await repository.GetAllAsync();

        Assert.DoesNotContain(secondRead, game => game.Title == "Mutated");
    }

    [Fact]
    public async Task GetPagedAsync_ReturnsRequestedPage()
    {
        var repository = CreateRepository();

        var result = await repository.GetPagedAsync(new GameFilter(), 2, 3);

        Assert.Equal(3, result.Items.Count);
        Assert.Equal(7, result.TotalCount);
        Assert.Equal(3, result.TotalPages);
        Assert.Equal(2, result.Page);
    }

    [Fact]
    public async Task GetPagedAsync_AppliesStatusAndSearchFilter()
    {
        var repository = CreateRepository();

        var byStatus = await repository.GetPagedAsync(
            new GameFilter(Status: GameStatus.Completed), 1, 10);
        var bySearch = await repository.GetPagedAsync(
            new GameFilter(Search: "elden"), 1, 10);

        Assert.Equal(2, byStatus.TotalCount);
        Assert.Single(bySearch.Items);
        Assert.Equal("Elden Ring", bySearch.Items[0].Title);
    }

    [Fact]
    public async Task GetPagedAsync_WithNullFilter_ThrowsArgumentNullException()
    {
        var repository = CreateRepository();

        await Assert.ThrowsAsync<ArgumentNullException>(
            () => repository.GetPagedAsync(null!, 1, 10));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task GetPagedAsync_WithInvalidPage_ThrowsArgumentOutOfRange(int page)
    {
        var repository = CreateRepository();

        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(
            () => repository.GetPagedAsync(new GameFilter(), page, 10));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-10)]
    public async Task GetPagedAsync_WithInvalidPageSize_ThrowsArgumentOutOfRange(int pageSize)
    {
        var repository = CreateRepository();

        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(
            () => repository.GetPagedAsync(new GameFilter(), 1, pageSize));
    }

    [Fact]
    public async Task GetByIdAsync_WithExistingId_ReturnsGame()
    {
        var repository = CreateRepository();
        var any = (await repository.GetAllAsync())[0];

        var found = await repository.GetByIdAsync(any.Id);

        Assert.NotNull(found);
        Assert.Equal(any.Title, found!.Title);
    }

    [Fact]
    public async Task GetByIdAsync_WithUnknownId_ReturnsNull()
    {
        var repository = CreateRepository();

        var found = await repository.GetByIdAsync(Guid.NewGuid());

        Assert.Null(found);
    }

    [Fact]
    public async Task GetByIdAsync_WithEmptyId_ThrowsArgumentException()
    {
        var repository = CreateRepository();

        await Assert.ThrowsAsync<ArgumentException>(() => repository.GetByIdAsync(Guid.Empty));
    }

    [Fact]
    public async Task AddAsync_AddsNewGame()
    {
        var repository = CreateRepository();
        var game = NewGame();

        var created = await repository.AddAsync(game);

        Assert.Equal(game.Id, created.Id);
        Assert.NotNull(await repository.GetByIdAsync(game.Id));
        Assert.Equal(8, (await repository.GetAllAsync()).Count);
    }

    [Fact]
    public async Task AddAsync_WithNullGame_ThrowsArgumentNullException()
    {
        var repository = CreateRepository();

        await Assert.ThrowsAsync<ArgumentNullException>(() => repository.AddAsync(null!));
    }

    [Fact]
    public async Task AddAsync_WithDuplicateTitle_ThrowsConflict()
    {
        var repository = CreateRepository();

        await Assert.ThrowsAsync<GameTrack.Domain.Common.ConflictException>(
            () => repository.AddAsync(NewGame("The Witcher 3: Wild Hunt")));
    }

    [Fact]
    public async Task AddAsync_WithDuplicateId_ThrowsConflict()
    {
        var repository = CreateRepository();
        var existing = (await repository.GetAllAsync())[0];
        var duplicate = Game.Restore(
            existing.Id,
            "Unique Title",
            "RPG",
            "PC",
            GameStatus.WantToPlay,
            null,
            null,
            null,
            null,
            existing.AddedAt,
            existing.UpdatedAt);

        await Assert.ThrowsAsync<GameTrack.Domain.Common.ConflictException>(
            () => repository.AddAsync(duplicate));
    }

    [Fact]
    public async Task UpdateAsync_ChangesStoredGame()
    {
        var repository = CreateRepository();
        var game = (await repository.GetAllAsync())[0];

        game.Update(game.ToDraft() with { Title = "Updated Title" });
        var updated = await repository.UpdateAsync(game);

        Assert.Equal("Updated Title", updated.Title);
        Assert.Equal("Updated Title", (await repository.GetByIdAsync(game.Id))!.Title);
    }

    [Fact]
    public async Task UpdateAsync_WithUnknownGame_ThrowsNotFound()
    {
        var repository = CreateRepository();

        await Assert.ThrowsAsync<GameTrack.Domain.Common.NotFoundException>(
            () => repository.UpdateAsync(NewGame()));
    }

    [Fact]
    public async Task UpdateAsync_WithNullGame_ThrowsArgumentNullException()
    {
        var repository = CreateRepository();

        await Assert.ThrowsAsync<ArgumentNullException>(() => repository.UpdateAsync(null!));
    }

    [Fact]
    public async Task DeleteAsync_RemovesExistingGame()
    {
        var repository = CreateRepository();
        var game = (await repository.GetAllAsync())[0];

        var deleted = await repository.DeleteAsync(game.Id);

        Assert.True(deleted);
        Assert.Null(await repository.GetByIdAsync(game.Id));
        Assert.Equal(6, (await repository.GetAllAsync()).Count);
    }

    [Fact]
    public async Task DeleteAsync_WithUnknownId_ReturnsFalse()
    {
        var repository = CreateRepository();

        Assert.False(await repository.DeleteAsync(Guid.NewGuid()));
    }

    [Fact]
    public async Task DeleteAsync_WithEmptyId_ThrowsArgumentException()
    {
        var repository = CreateRepository();

        await Assert.ThrowsAsync<ArgumentException>(() => repository.DeleteAsync(Guid.Empty));
    }

    [Fact]
    public async Task ExistsByTitleAsync_IsCaseInsensitive()
    {
        var repository = CreateRepository();

        Assert.True(await repository.ExistsByTitleAsync("the witcher 3: wild hunt"));
        Assert.False(await repository.ExistsByTitleAsync("Unknown Game"));
    }

    [Fact]
    public async Task ExistsByTitleAsync_WithEmptyTitle_ThrowsArgumentException()
    {
        var repository = CreateRepository();

        await Assert.ThrowsAsync<ArgumentException>(() => repository.ExistsByTitleAsync(" "));
    }

    [Fact]
    public async Task ExistsByTitleAsync_WithEmptyExcludeId_ThrowsArgumentException()
    {
        var repository = CreateRepository();

        await Assert.ThrowsAsync<ArgumentException>(
            () => repository.ExistsByTitleAsync("Some Game", Guid.Empty));
    }
}
