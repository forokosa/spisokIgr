using GameTrack.Domain.Services;
using GameTrack.Infrastructure.Persistence;

namespace GameTrack.Tests.Domain;

public sealed class StatisticsServiceTests
{
    [Fact]
    public async Task GetStatisticsAsync_WithSeedData_ReturnsExpectedAggregates()
    {
        var service = new StatisticsService(new InMemoryGameRepository());

        var stats = await service.GetStatisticsAsync();

        Assert.Equal(7, stats.Total);
        Assert.Equal(2, stats.Completed);
        Assert.Equal(2, stats.InProgress);
        Assert.Equal(2, stats.WantToPlay);
        Assert.Equal(1, stats.Dropped);
        Assert.Equal(29, stats.CompletionRate);
        Assert.Equal(8.8m, stats.AverageRating);
        Assert.Equal(4, stats.ByStatus.Count);
        Assert.Equal(2, stats.ByGenre.First(item => item.Genre == "RPG").Count);
        Assert.Equal(4, stats.ByPlatform.First(item => item.Platform == "PC").Count);
    }

    [Fact]
    public async Task GetStatisticsAsync_WithEmptyRepository_ReturnsZeros()
    {
        var service = new StatisticsService(new InMemoryGameRepository([]));

        var stats = await service.GetStatisticsAsync();

        Assert.Equal(0, stats.Total);
        Assert.Equal(0, stats.CompletionRate);
        Assert.Null(stats.AverageRating);
    }

    [Fact]
    public async Task Constructor_WithNullRepository_ThrowsArgumentNullException()
    {
        await Assert.ThrowsAsync<ArgumentNullException>(
            () => Task.FromResult(new StatisticsService(null!)));
    }

    [Fact]
    public async Task Calculate_WithNullGames_ThrowsArgumentNullException()
    {
        await Assert.ThrowsAsync<ArgumentNullException>(
            () => Task.FromResult(LibraryStatisticsCalculator.Calculate(null!)));
    }
}
