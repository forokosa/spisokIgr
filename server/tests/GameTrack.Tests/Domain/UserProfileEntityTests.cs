using GameTrack.Domain.Entities;
using GameTrack.Domain.Models;

namespace GameTrack.Tests.Domain;

public sealed class UserProfileEntityTests
{
    private static UserProfile CreateProfile() => UserProfile.Restore(
        Guid.NewGuid(),
        "Алексей Игрок",
        "demo@gametrack.app",
        "images/avatar.svg",
        new DateTimeOffset(2024, 1, 1, 9, 0, 0, TimeSpan.Zero),
        new DateTimeOffset(2024, 1, 1, 9, 0, 0, TimeSpan.Zero));

    [Fact]
    public async Task Update_WithValidData_ChangesProfile()
    {
        var profile = CreateProfile();

        profile.Update(new ProfileDraft("Новое Имя", "new@example.com", null));

        Assert.Equal("Новое Имя", profile.Name);
        Assert.Equal("new@example.com", profile.Email);
        Assert.Null(profile.AvatarUrl);
    }

    [Fact]
    public async Task Update_WithNullDraft_ThrowsArgumentNullException()
    {
        var profile = CreateProfile();

        await Assert.ThrowsAsync<ArgumentNullException>(() => Task.Run(() => profile.Update(null!)));
    }

    [Theory]
    [InlineData("")]
    [InlineData("A")]
    [InlineData("  ")]
    public async Task Update_WithInvalidName_ThrowsArgumentException(string name)
    {
        var profile = CreateProfile();

        await Assert.ThrowsAsync<ArgumentException>(
            () => Task.Run(() => profile.Update(new ProfileDraft(name, "valid@example.com"))));
    }

    [Theory]
    [InlineData("")]
    [InlineData("not-an-email")]
    [InlineData("a@b")]
    [InlineData("a b@example.com")]
    public async Task Update_WithInvalidEmail_ThrowsArgumentException(string email)
    {
        var profile = CreateProfile();

        await Assert.ThrowsAsync<ArgumentException>(
            () => Task.Run(() => profile.Update(new ProfileDraft("Valid Name", email))));
    }

    [Fact]
    public async Task Update_WithInvalidAvatarUrl_ThrowsArgumentException()
    {
        var profile = CreateProfile();

        await Assert.ThrowsAsync<ArgumentException>(() => Task.Run(() => 
            profile.Update(new ProfileDraft("Valid Name", "valid@example.com", "javascript:alert(1)"))));
    }
}
