using GameTrack.Domain.Repositories;
using GameTrack.Domain.Services;
using GameTrack.Tests.Fakes;

namespace GameTrack.Tests.Domain;

public sealed class StatisticsServiceTests
{
    private static readonly UserContext DemoUser = new(TestSeed.ProfileId, TestSeed.Login);

    [Fact]
    public async Task GetStatisticsAsync_WithSeedData_ReturnsExpectedAggregates()
    {
        var repository = new FakeGameRepository(DemoUser.UserId, TestSeed.CreateGames());
        var service = new StatisticsService(repository, new FakeUserContextProvider(DemoUser));

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
        var repository = new FakeGameRepository();
        var service = new StatisticsService(repository, new FakeUserContextProvider(DemoUser));

        var stats = await service.GetStatisticsAsync();

        Assert.Equal(0, stats.Total);
        Assert.Equal(0, stats.CompletionRate);
        Assert.Null(stats.AverageRating);
    }

    [Fact]
    public async Task GetStatisticsAsync_IsolatedPerUser()
    {
        var repository = new FakeGameRepository(DemoUser.UserId, TestSeed.CreateGames());
        var service = new StatisticsService(
            repository,
            new FakeUserContextProvider(Guid.NewGuid(), "someone"));

        var stats = await service.GetStatisticsAsync();

        Assert.Equal(0, stats.Total);
    }

    [Fact]
    public async Task Constructor_WithNullRepository_ThrowsArgumentNullException()
    {
        var users = new FakeUserContextProvider(DemoUser);

        await Assert.ThrowsAsync<ArgumentNullException>(
            () => Task.FromResult(new StatisticsService(null!, users)));
    }

    [Fact]
    public async Task Constructor_WithNullUserProvider_ThrowsArgumentNullException()
    {
        var repository = new FakeGameRepository(DemoUser.UserId, TestSeed.CreateGames());

        await Assert.ThrowsAsync<ArgumentNullException>(
            () => Task.FromResult(new StatisticsService(repository, null!)));
    }

    [Fact]
    public async Task Calculate_WithNullGames_ThrowsArgumentNullException()
    {
        await Assert.ThrowsAsync<ArgumentNullException>(
            () => Task.FromResult(LibraryStatisticsCalculator.Calculate(null!)));
    }
}
