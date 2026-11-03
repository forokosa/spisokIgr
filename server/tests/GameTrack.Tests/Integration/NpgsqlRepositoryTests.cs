using GameTrack.Domain.Common;
using GameTrack.Domain.Models;
using GameTrack.Domain.Repositories;
using GameTrack.Infrastructure.Persistence;
using GameTrack.Infrastructure.Security;
using Npgsql;

namespace GameTrack.Tests.Integration;

/// <summary>
/// Интеграционные тесты SQL-репозиториев (IAccountRepository,
/// IUserProfileRepository) на реальном PostgreSQL.
/// </summary>
[Collection(PostgresCollection.Name)]
public sealed class NpgsqlAccountRepositoryTests
{
    private readonly PostgresFixture _fixture;

    public NpgsqlAccountRepositoryTests(PostgresFixture fixture)
    {
        _fixture = fixture;
    }

    private NpgsqlAccountRepository CreateRepository()
        => new(_fixture.DataSource);

    [Fact]
    public async Task DemoAccount_IsFoundAndPasswordVerified()
    {
        PostgresSkip.Require();
        var repository = CreateRepository();

        Assert.True(await repository.ExistsAsync("demo@gametrack.app"));

        var account = await repository.FindAsync("DEMO@gametrack.app");
        Assert.NotNull(account);
        Assert.Equal("demo@gametrack.app", account!.Email);
        Assert.Equal("Алексей Игрок", account.Name);
        Assert.Equal(AccountIdMap.DemoUserId, account.Id);

        Assert.True(await repository.ValidateAsync("demo@gametrack.app", "demo1234"));
        Assert.False(await repository.ValidateAsync("demo@gametrack.app", "wrong-password"));
    }

    [Fact]
    public async Task RegisterAsync_PersistsAccountWithHashedPassword()
    {
        PostgresSkip.Require();
        var repository = CreateRepository();
        var email = $"player-{Guid.NewGuid():N}@gametrack.app";

        var account = await repository.RegisterAsync("Новый Игрок", email, "secret123");

        Assert.NotEqual(Guid.Empty, account.Id);
        Assert.Equal(email, account.Email);
        Assert.Equal("Новый Игрок", account.Name);

        Assert.True(await repository.ExistsAsync(email));
        Assert.True(await repository.ValidateAsync(email, "secret123"));
        Assert.False(await repository.ValidateAsync(email, "secret124"));

        // Пароль в БД хранится только в виде PBKDF2-хеша.
        await using var connection = await _fixture.DataSource.OpenConnectionAsync();
        await using var command = new NpgsqlCommand(
            "SELECT password_hash FROM accounts WHERE lower(email) = lower(@email)",
            connection);
        command.Parameters.AddWithValue("email", email);
        var stored = (string)(await command.ExecuteScalarAsync())!;

        Assert.StartsWith("pbkdf2-sha256$", stored, StringComparison.Ordinal);
        Assert.DoesNotContain("secret123", stored, StringComparison.Ordinal);
    }

    [Fact]
    public async Task RegisterAsync_WithDuplicateEmail_ThrowsConflictFromUniqueViolation()
    {
        PostgresSkip.Require();
        var repository = CreateRepository();

        await Assert.ThrowsAsync<ConflictException>(
            () => repository.RegisterAsync("Второй", "demo@gametrack.app", "password456"));
    }

    [Theory]
    [InlineData("A", "valid@gametrack.app", "password123")]
    [InlineData("Новый Игрок", "bad", "password123")]
    [InlineData("Новый Игрок", "valid@gametrack.app", "short")]
    public async Task RegisterAsync_WithInvalidData_ThrowsArgumentException(
        string name,
        string email,
        string password)
    {
        PostgresSkip.Require();

        await Assert.ThrowsAnyAsync<ArgumentException>(
            () => CreateRepository().RegisterAsync(name, email, password));
    }

    [Fact]
    public async Task FindLoginByPublicIdAsync_ResolvesLogin()
    {
        PostgresSkip.Require();
        var repository = CreateRepository();

        var login = await repository.FindLoginByPublicIdAsync(
            AccountIdMap.ToPublicId(AccountIdMap.DemoLogin));

        Assert.Equal(AccountIdMap.DemoLogin, login);
        Assert.Null(await repository.FindLoginByPublicIdAsync(Guid.NewGuid()));
    }

    [Fact]
    public async Task ValidateAsync_WithUnknownEmail_ReturnsFalse()
    {
        PostgresSkip.Require();

        Assert.False(await CreateRepository().ValidateAsync("unknown@gametrack.app", "password123"));
    }

    [Fact]
    public async Task AccountQueries_AreParameterizedAgainstSqlInjection()
    {
        PostgresSkip.Require();
        var repository = CreateRepository();

        // Инъекция попала бы в WHERE как значение: строка не находится.
        var account = await repository.FindAsync("demo@gametrack.app' OR '1'='1");

        Assert.Null(account);
    }
}

/// <summary>Интеграционные тесты SQL-репозитория профиля.</summary>
[Collection(PostgresCollection.Name)]
public sealed class NpgsqlUserProfileRepositoryTests
{
    private readonly PostgresFixture _fixture;

    public NpgsqlUserProfileRepositoryTests(PostgresFixture fixture)
    {
        _fixture = fixture;
    }

    /// <summary>Создаёт пользователя; без доступной БД тест завершается как пропущенный.</summary>
    private async Task<UserContext> RequireUserAsync(string login, string email, string displayName)
    {
        PostgresSkip.Require();
        return await _fixture.CreateUserAsync(login, email, displayName);
    }

    [Fact]
    public async Task GetAsync_ReturnsDemoProfileFromAccountsAndUserProfiles()
    {
        PostgresSkip.Require();
        var repository = new NpgsqlUserProfileRepository(_fixture.DataSource);

        var profile = await repository.GetAsync(PostgresFixture.DemoUser);

        Assert.NotNull(profile);
        Assert.Equal(AccountIdMap.DemoUserId, profile!.Id);
        Assert.Equal("Алексей Игрок", profile.Name);
        Assert.Equal("demo@gametrack.app", profile.Email);
        Assert.Equal("images/avatar.svg", profile.AvatarUrl);
    }

    [Fact]
    public async Task GetAsync_ForUnknownUser_ReturnsNull()
    {
        PostgresSkip.Require();
        var repository = new NpgsqlUserProfileRepository(_fixture.DataSource);

        var profile = await repository.GetAsync(new UserContext(Guid.NewGuid(), "nobody"));

        Assert.Null(profile);
    }

    [Fact]
    public async Task UpdateAsync_ChangesNameAvatarAndEmailTogether()
    {
        PostgresSkip.Require();
        var user = await RequireUserAsync(
            $"profile-{Guid.NewGuid():N}"[..20],
            $"profile-{Guid.NewGuid():N}@gametrack.app",
            "Старое Имя");

        var repository = new NpgsqlUserProfileRepository(_fixture.DataSource);
        var profile = await repository.GetAsync(user);
        Assert.NotNull(profile);

        var newEmail = $"updated-{Guid.NewGuid():N}@gametrack.app";
        profile!.Update(new ProfileDraft("Новое Имя", newEmail, "images/new-avatar.svg"));

        var updated = await repository.UpdateAsync(user, profile);

        Assert.Equal("Новое Имя", updated.Name);
        Assert.Equal(newEmail, updated.Email);
        Assert.Equal("images/new-avatar.svg", updated.AvatarUrl);

        var reread = await repository.GetAsync(user);
        Assert.Equal("Новое Имя", reread!.Name);
        Assert.Equal(newEmail, reread.Email);
    }

    [Fact]
    public async Task UpdateAsync_WithForeignContext_ThrowsNotFound()
    {
        PostgresSkip.Require();
        var foreignUser = new UserContext(Guid.NewGuid(), $"nobody-{Guid.NewGuid():N}"[..20]);
        var repository = new NpgsqlUserProfileRepository(_fixture.DataSource);
        var profile = await repository.GetAsync(PostgresFixture.DemoUser);

        await Assert.ThrowsAsync<NotFoundException>(
            () => repository.UpdateAsync(foreignUser, profile!));
    }

    [Fact]
    public async Task CreateAsync_CreatesProfileForRegisteredAccount()
    {
        PostgresSkip.Require();
        var accounts = new NpgsqlAccountRepository(_fixture.DataSource);
        var email = $"created-{Guid.NewGuid():N}@gametrack.app";
        var account = await accounts.RegisterAsync("Свежий Профиль", email, "password123");

        var repository = new NpgsqlUserProfileRepository(_fixture.DataSource);
        var login = await accounts.FindLoginByPublicIdAsync(account.Id);

        await repository.CreateAsync(
            GameTrack.Domain.Entities.UserProfile.Restore(
                account.Id,
                "Свежий Профиль",
                email,
                "images/avatar.svg",
                DateTimeOffset.UtcNow,
                DateTimeOffset.UtcNow));

        var profile = await repository.GetAsync(new UserContext(account.Id, login!));
        Assert.NotNull(profile);
        Assert.Equal("Свежий Профиль", profile!.Name);
        Assert.Equal(email, profile.Email);
    }

    [Fact]
    public async Task UpdateAsync_WithDuplicateEmail_ThrowsConflict()
    {
        PostgresSkip.Require();
        var user = await RequireUserAsync(
            $"dup-{Guid.NewGuid():N}"[..20],
            $"dup-{Guid.NewGuid():N}@gametrack.app",
            "Дубль");

        var repository = new NpgsqlUserProfileRepository(_fixture.DataSource);
        var profile = await repository.GetAsync(user);
        profile!.Update(new ProfileDraft("Дубль", "demo@gametrack.app", null));

        await Assert.ThrowsAsync<ConflictException>(() => repository.UpdateAsync(user, profile));
    }
}
