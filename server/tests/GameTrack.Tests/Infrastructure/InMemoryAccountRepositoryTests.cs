using GameTrack.Domain.Common;
using GameTrack.Infrastructure.Security;

namespace GameTrack.Tests.Infrastructure;

public sealed class InMemoryAccountRepositoryTests
{
    [Fact]
    public async Task Constructor_WithSeed_AllowsDemoLogin()
    {
        var repository = new InMemoryAccountRepository("demo@gametrack.app", "demo1234");

        Assert.True(await repository.ExistsAsync("demo@gametrack.app"));
        Assert.True(await repository.ValidateAsync("demo@gametrack.app", "demo1234"));
        Assert.False(await repository.ValidateAsync("demo@gametrack.app", "wrong-password"));
    }

    [Fact]
    public async Task RegisterAsync_AddsAccountAndHashesPassword()
    {
        var repository = new InMemoryAccountRepository();

        var account = await repository.RegisterAsync(
            "Новый Игрок",
            "player@gametrack.app",
            "secret123");

        Assert.Equal("Новый Игрок", account.Name);
        Assert.Equal("player@gametrack.app", account.Email);
        Assert.NotEqual(Guid.Empty, account.Id);
        Assert.True(await repository.ExistsAsync("PLAYER@gametrack.app"));
        Assert.True(await repository.ValidateAsync("player@gametrack.app", "secret123"));
        Assert.False(await repository.ValidateAsync("player@gametrack.app", "secret124"));
    }

    [Fact]
    public async Task RegisterAsync_WithDuplicateEmail_ThrowsConflict()
    {
        var repository = new InMemoryAccountRepository();
        await repository.RegisterAsync("Первый", "dup@gametrack.app", "password123");

        await Assert.ThrowsAsync<ConflictException>(
            () => repository.RegisterAsync("Второй", "DUP@gametrack.app", "password456"));
    }

    [Theory]
    [InlineData("A", "player@gametrack.app", "password123")]
    [InlineData("Новый Игрок", "bad", "password123")]
    [InlineData("Новый Игрок", "player@gametrack.app", "short")]
    public async Task RegisterAsync_WithInvalidData_ThrowsArgumentException(
        string name,
        string email,
        string password)
    {
        var repository = new InMemoryAccountRepository();

        await Assert.ThrowsAnyAsync<ArgumentException>(
            () => repository.RegisterAsync(name, email, password));
    }

    [Fact]
    public async Task FindAsync_ReturnsNullForUnknownEmail()
    {
        var repository = new InMemoryAccountRepository();

        Assert.Null(await repository.FindAsync("unknown@gametrack.app"));
    }

    [Fact]
    public async Task ValidateAsync_WithNullPassword_ThrowsArgumentNullException()
    {
        var repository = new InMemoryAccountRepository();

        await Assert.ThrowsAsync<ArgumentNullException>(
            () => repository.ValidateAsync("player@gametrack.app", null!));
    }
}
