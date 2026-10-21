using GameTrack.Domain.Models;
using GameTrack.Infrastructure.Persistence;

namespace GameTrack.Tests.Infrastructure;

public sealed class InMemoryUserProfileRepositoryTests
{
    [Fact]
    public async Task GetAsync_ReturnsSeededProfile()
    {
        var repository = new InMemoryUserProfileRepository();

        var profile = await repository.GetAsync();

        Assert.NotNull(profile);
        Assert.Equal("demo@gametrack.app", profile!.Email);
    }

    [Fact]
    public async Task UpdateAsync_WithValidProfile_UpdatesData()
    {
        var repository = new InMemoryUserProfileRepository();
        var profile = (await repository.GetAsync())!;
        profile.Update(new ProfileDraft("Новое Имя", "new@example.com"));

        var updated = await repository.UpdateAsync(profile);

        Assert.Equal("Новое Имя", updated.Name);
        Assert.Equal("new@example.com", updated.Email);
        Assert.Equal("Новое Имя", (await repository.GetAsync())!.Name);
    }

    [Fact]
    public async Task UpdateAsync_WithNullProfile_ThrowsArgumentNullException()
    {
        var repository = new InMemoryUserProfileRepository();

        await Assert.ThrowsAsync<ArgumentNullException>(() => repository.UpdateAsync(null!));
    }

    [Fact]
    public async Task UpdateAsync_WithForeignId_ThrowsArgumentException()
    {
        var repository = new InMemoryUserProfileRepository();
        var foreign = GameTrack.Domain.Entities.UserProfile.Restore(
            Guid.NewGuid(),
            "Другой Пользователь",
            "other@example.com",
            null,
            new DateTimeOffset(2024, 1, 1, 0, 0, 0, TimeSpan.Zero),
            new DateTimeOffset(2024, 1, 1, 0, 0, 0, TimeSpan.Zero));

        await Assert.ThrowsAsync<ArgumentException>(() => repository.UpdateAsync(foreign));
    }
}
