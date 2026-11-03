using GameTrack.Domain.Common;
using GameTrack.Domain.Models;
using GameTrack.Domain.Services;
using GameTrack.Tests.Fakes;

namespace GameTrack.Tests.Domain;

public sealed class AuthServiceTests
{
    private static AuthService CreateService(
        out FakeUserProfileRepository profiles,
        out FakeAccountRepository accounts)
    {
        profiles = new FakeUserProfileRepository();
        accounts = new FakeAccountRepository(
            "demo@gametrack.app",
            "demo1234",
            id: AccountIdMapForDemo());

        return new AuthService(profiles, accounts);
    }

    private static Guid AccountIdMapForDemo()
        => GameTrack.Infrastructure.Persistence.AccountIdMap.DemoUserId;

    private static RegisterDraft Draft(
        string name = "Новый Игрок",
        string email = "new@gametrack.app",
        string password = "password123",
        bool remember = true)
        => new(name, email, password, remember);

    [Fact]
    public async Task Constructor_WithNullProfiles_ThrowsArgumentNullException()
    {
        await Assert.ThrowsAsync<ArgumentNullException>(
            () => Task.FromResult(new AuthService(null!, new FakeAccountRepository())));
    }

    [Fact]
    public async Task Constructor_WithNullAccounts_ThrowsArgumentNullException()
    {
        await Assert.ThrowsAsync<ArgumentNullException>(
            () => Task.FromResult(new AuthService(new FakeUserProfileRepository(), null!)));
    }

    [Fact]
    public async Task RegisterAsync_WithValidData_CreatesAccountAndSession()
    {
        var service = CreateService(out var profiles, out _);

        var session = await service.RegisterAsync(Draft());

        Assert.Equal("new@gametrack.app", session.Email);
        Assert.Equal("Новый Игрок", session.Name);
        Assert.True(session.Remember);
        Assert.NotEqual(Guid.Empty, session.UserId);

        var profile = await profiles.GetAsync(new GameTrack.Domain.Repositories.UserContext(session.UserId, "new"));
        Assert.NotNull(profile);
        Assert.Equal("new@gametrack.app", profile!.Email);
        Assert.Equal("Новый Игрок", profile.Name);
    }

    [Fact]
    public async Task RegisterAsync_WithDuplicateEmail_ThrowsConflict()
    {
        var service = CreateService(out _, out _);
        await service.RegisterAsync(Draft(email: "dup@gametrack.app"));

        await Assert.ThrowsAsync<ConflictException>(
            () => service.RegisterAsync(Draft(email: "DUP@gametrack.app")));
    }

    [Theory]
    [InlineData("", "new@gametrack.app", "password123")]
    [InlineData("A", "new@gametrack.app", "password123")]
    [InlineData("Новый Игрок", "bad-email", "password123")]
    [InlineData("Новый Игрок", "new@gametrack.app", "short")]
    [InlineData("Новый Игрок", "new@gametrack.app", "12345678")]
    [InlineData("Новый Игрок", "new@gametrack.app", "onlyletters")]
    public async Task RegisterAsync_WithInvalidData_ThrowsArgumentException(
        string name,
        string email,
        string password)
    {
        var service = CreateService(out _, out _);

        await Assert.ThrowsAnyAsync<ArgumentException>(
            () => service.RegisterAsync(Draft(name, email, password)));
    }

    [Fact]
    public async Task LoginAsync_AfterRegistration_ReturnsSession()
    {
        var service = CreateService(out _, out _);
        await service.RegisterAsync(Draft(email: "player@gametrack.app", password: "secret123"));

        var session = await service.LoginAsync("player@gametrack.app", "secret123", false);

        Assert.Equal("player@gametrack.app", session.Email);
        Assert.Equal("Новый Игрок", session.Name);
        Assert.False(session.Remember);
    }

    [Fact]
    public async Task LoginAsync_WithUnknownAccount_ThrowsUnauthorized()
    {
        var service = CreateService(out _, out _);

        await Assert.ThrowsAsync<UnauthorizedException>(
            () => service.LoginAsync("unknown@gametrack.app", "password123", false));
    }

    [Fact]
    public async Task LoginAsync_DoesNotOverwriteOtherProfiles()
    {
        var service = CreateService(out var profiles, out _);

        var registered = await service.RegisterAsync(Draft());
        var demoSession = await service.LoginAsync("demo@gametrack.app", "demo1234", true);

        // Раньше вход демо-пользователя перезаписывал единственный профиль.
        var registeredProfile = await profiles.GetAsync(
            new GameTrack.Domain.Repositories.UserContext(registered.UserId, "new"));
        Assert.NotNull(registeredProfile);
        Assert.Equal("Новый Игрок", registeredProfile!.Name);
        Assert.NotEqual(demoSession.UserId, registered.UserId);
    }

    [Fact]
    public async Task RequestRecoveryAsync_WithUnknownEmail_ThrowsNotFound()
    {
        var service = CreateService(out _, out _);

        await Assert.ThrowsAsync<NotFoundException>(
            () => service.RequestRecoveryAsync("unknown@gametrack.app"));
    }
}
