using GameTrack.Domain.Common;
using GameTrack.Domain.Models;
using GameTrack.Domain.Services;
using GameTrack.Infrastructure.Persistence;
using GameTrack.Infrastructure.Security;

namespace GameTrack.Tests.Domain;

public sealed class AuthServiceTests
{
    private static AuthService CreateService(
        out InMemoryUserProfileRepository profiles,
        out InMemoryAccountRepository accounts)
    {
        profiles = new InMemoryUserProfileRepository();
        accounts = new InMemoryAccountRepository(
            "demo@gametrack.app",
            "demo1234",
            id: SeedData.ProfileId);
        return new AuthService(profiles, accounts);
    }

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
            () => Task.FromResult(new AuthService(
                null!,
                new InMemoryAccountRepository())));
    }

    [Fact]
    public async Task Constructor_WithNullAccounts_ThrowsArgumentNullException()
    {
        await Assert.ThrowsAsync<ArgumentNullException>(
            () => Task.FromResult(new AuthService(
                new InMemoryUserProfileRepository(),
                null!)));
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
        Assert.NotEqual(SeedData.ProfileId, session.UserId);
        Assert.Equal("new@gametrack.app", (await profiles.GetAsync())!.Email);
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
    public async Task LoginAsync_WithDemoAccount_RestoresDemoProfile()
    {
        var service = CreateService(out var profiles, out _);
        await service.RegisterAsync(Draft());
        Assert.Equal("Новый Игрок", (await profiles.GetAsync())!.Name);

        var session = await service.LoginAsync("demo@gametrack.app", "demo1234", true);

        Assert.Equal("Алексей Игрок", session.Name);
        Assert.Equal(SeedData.ProfileId, session.UserId);
        Assert.Equal("Алексей Игрок", (await profiles.GetAsync())!.Name);
    }

    [Fact]
    public async Task RequestRecoveryAsync_WithUnknownEmail_ThrowsNotFound()
    {
        var service = CreateService(out _, out _);

        await Assert.ThrowsAsync<NotFoundException>(
            () => service.RequestRecoveryAsync("unknown@gametrack.app"));
    }
}
