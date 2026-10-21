using GameTrack.Domain.Common;
using GameTrack.Domain.Entities;
using GameTrack.Domain.Enums;
using GameTrack.Domain.Models;

namespace GameTrack.Tests.Domain;

public sealed class GameEntityTests
{
    private static GameDraft ValidDraft() => new(
        "Test Game",
        "RPG",
        "PC",
        GameStatus.WantToPlay,
        "images/covers/test.svg",
        "Описание",
        8.5m,
        2020);

    [Fact]
    public async Task Create_WithValidData_InitializesEntity()
    {
        var now = new DateTimeOffset(2025, 1, 2, 3, 4, 5, TimeSpan.Zero);

        var game = await Task.FromResult(Game.Create(ValidDraft(), now));

        Assert.NotEqual(Guid.Empty, game.Id);
        Assert.Equal("Test Game", game.Title);
        Assert.Equal("RPG", game.Genre);
        Assert.Equal("PC", game.Platform);
        Assert.Equal(GameStatus.WantToPlay, game.Status);
        Assert.Equal(8.5m, game.Rating);
        Assert.Equal(2020, game.ReleaseYear);
        Assert.Equal(now, game.AddedAt);
        Assert.Equal(now, game.UpdatedAt);
    }

    [Fact]
    public async Task Create_WithNullDraft_ThrowsArgumentNullException()
    {
        await Assert.ThrowsAsync<ArgumentNullException>(() => Task.FromResult(Game.Create(null!)));
    }

    [Theory]
    [InlineData("")]
    [InlineData(" A ")]
    [InlineData("   ")]
    public async Task Create_WithTooShortTitle_ThrowsArgumentException(string title)
    {
        var draft = ValidDraft() with { Title = title };

        await Assert.ThrowsAsync<ArgumentException>(() => Task.FromResult(Game.Create(draft)));
    }

    [Fact]
    public async Task Create_WithTooLongTitle_ThrowsArgumentOutOfRangeException()
    {
        var draft = ValidDraft() with { Title = new string('a', 201) };

        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => Task.FromResult(Game.Create(draft)));
    }

    [Theory]
    [InlineData("")]
    [InlineData("  ")]
    public async Task Create_WithBlankGenre_ThrowsArgumentException(string genre)
    {
        var draft = ValidDraft() with { Genre = genre };

        await Assert.ThrowsAsync<ArgumentException>(() => Task.FromResult(Game.Create(draft)));
    }

    [Theory]
    [InlineData(-0.1)]
    [InlineData(10.1)]
    [InlineData(100)]
    public async Task Create_WithInvalidRating_ThrowsArgumentOutOfRangeException(decimal rating)
    {
        var draft = ValidDraft() with { Rating = rating };

        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => Task.FromResult(Game.Create(draft)));
    }

    [Theory]
    [InlineData(1969)]
    [InlineData(2101)]
    public async Task Create_WithInvalidReleaseYear_ThrowsArgumentOutOfRangeException(int year)
    {
        var draft = ValidDraft() with { ReleaseYear = year };

        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => Task.FromResult(Game.Create(draft)));
    }

    [Fact]
    public async Task Create_WithUndefinedStatus_ThrowsArgumentOutOfRangeException()
    {
        var draft = ValidDraft() with { Status = (GameStatus)999 };

        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => Task.FromResult(Game.Create(draft)));
    }

    [Fact]
    public async Task Create_WithTooLongDescription_ThrowsArgumentOutOfRangeException()
    {
        var draft = ValidDraft() with { Description = new string('d', 2001) };

        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => Task.FromResult(Game.Create(draft)));
    }

    [Fact]
    public async Task Create_WithInvalidCoverUrl_ThrowsArgumentException()
    {
        var draft = ValidDraft() with { CoverUrl = "ftp://example.com/cover.png" };

        await Assert.ThrowsAsync<ArgumentException>(() => Task.FromResult(Game.Create(draft)));
    }

    [Fact]
    public async Task Update_WithValidDraft_ChangesFieldsAndUpdatedAt()
    {
        var createdAt = new DateTimeOffset(2025, 1, 1, 0, 0, 0, TimeSpan.Zero);
        var updatedAt = createdAt.AddDays(2);
        var game = Game.Create(ValidDraft(), createdAt);

        game.Update(ValidDraft() with
        {
            Title = "Updated Game",
            Status = GameStatus.Completed,
            Rating = 10m
        }, updatedAt);

        Assert.Equal("Updated Game", game.Title);
        Assert.Equal(GameStatus.Completed, game.Status);
        Assert.Equal(10m, game.Rating);
        Assert.Equal(updatedAt, game.UpdatedAt);
    }

    [Fact]
    public async Task Update_WithNullDraft_ThrowsArgumentNullException()
    {
        var game = Game.Create(ValidDraft());

        await Assert.ThrowsAsync<ArgumentNullException>(() => Task.Run(() => game.Update(null!)));
    }

    [Fact]
    public async Task ChangeStatus_WithInvalidStatus_ThrowsArgumentOutOfRangeException()
    {
        var game = Game.Create(ValidDraft());

        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(
            () => Task.Run(() => game.ChangeStatus((GameStatus)0)));
    }

    [Fact]
    public async Task ChangeStatus_WithValidStatus_UpdatesEntity()
    {
        var game = Game.Create(ValidDraft());

        game.ChangeStatus(GameStatus.Completed);

        Assert.Equal(GameStatus.Completed, game.Status);
    }

    [Fact]
    public async Task Restore_WithDefaultDate_ThrowsArgumentException()
    {
        await Assert.ThrowsAsync<ArgumentException>(() => Task.FromResult(Game.Restore(
            Guid.NewGuid(),
            "Game",
            "RPG",
            "PC",
            GameStatus.Completed,
            null,
            null,
            null,
            null,
            default,
            default)));
    }
}
