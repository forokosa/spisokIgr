using GameTrack.Domain.Entities;
using GameTrack.Domain.Enums;
using GameTrack.Domain.Models;
using GameTrack.Domain.Repositories;
using GameTrack.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Npgsql;
using Npgsql.NameTranslation;

namespace GameTrack.Tests.Integration;

/// <summary>
/// Общая база для классов интеграционных тестов одного прогона:
/// создаётся один раз через <see cref="PostgresTestDatabase.CreateAsync"/>
/// и удаляется после завершения. Это коллекция xUnit, поэтому разные
/// классы тестов не мешают друг другу.
///
/// Если PostgreSQL недоступен, коллекция помечается как недоступная и
/// тесты пропускаются.
/// </summary>
public sealed class PostgresFixture : IAsyncLifetime
{
    private PostgresTestDatabase? _database;

    public bool IsAvailable { get; private set; }

    public string ConnectionString =>
        _database?.ConnectionString
        ?? throw new InvalidOperationException("Тестовая база не создана.");

    public NpgsqlDataSource DataSource { get; private set; } = null!;

    public async Task InitializeAsync()
    {
        IsAvailable = await PostgresTestDatabase.IsAvailableAsync();
        PostgresSkip.Available = IsAvailable;

        if (!IsAvailable)
        {
            return;
        }

        _database = await PostgresTestDatabase.CreateAsync();
        DataSource = NpgsqlDataSource.Create(_database.ConnectionString);
    }

    public async Task DisposeAsync()
    {
        if (DataSource is not null)
        {
            await DataSource.DisposeAsync();
        }

        if (_database is not null)
        {
            await _database.DropAsync();
        }
    }

    /// <summary>Строка подключения, к которой привязан экземпляр БД (для диагностики).</summary>
    public string DatabaseName => _database?.DatabaseName ?? "(нет)";

    /// <summary>Демо-пользователь, созданный скриптом db/003_seed_demo_data.sql.</summary>
    public static UserContext DemoUser { get; } = new(AccountIdMap.DemoUserId, AccountIdMap.DemoLogin);

    /// <summary>Создаёт отдельного пользователя со своей библиотекой.</summary>
    public async Task<UserContext> CreateUserAsync(string login, string email, string displayName)
    {
        await using var connection = await DataSource.OpenConnectionAsync();

        var accountId = (long)(await new NpgsqlCommand(
                """
                INSERT INTO accounts (login, email, password_hash)
                VALUES (@login, @email, 'x')
                RETURNING id
                """,
                connection)
            .WithParameters(("login", login), ("email", email))
            .ExecuteScalarAsync())!;

        await new NpgsqlCommand(
                """
                INSERT INTO user_profiles (account_id, display_name)
                VALUES (@account_id, @display_name)
                """,
                connection)
            .WithParameters(("account_id", accountId), ("display_name", displayName))
            .ExecuteNonQueryAsync();

        return new UserContext(AccountIdMap.ToPublicId(login), login);
    }
}

/// <summary>Параметры для <see cref="NpgsqlCommand"/> без длинных цепочек AddWithValue.</summary>
internal static class NpgsqlCommandExtensions
{
    public static NpgsqlCommand WithParameters(this NpgsqlCommand command, params (string Name, object? Value)[] parameters)
    {
        foreach (var (name, value) in parameters)
        {
            command.Parameters.AddWithValue(name, value ?? DBNull.Value);
        }

        return command;
    }
}

/// <summary>
/// Контекст и репозиторий, привязанные к строке подключения.
/// Dispose освобождает DbContext, как и в реальном запросе.
/// </summary>
public sealed class LibraryScope : IAsyncDisposable
{
    private readonly GameTrackDbContext _context;

    public LibraryScope(string connectionString, UserContext user)
    {
        var options = new Microsoft.EntityFrameworkCore.DbContextOptionsBuilder<GameTrackDbContext>()
            .UseNpgsql(connectionString)
            .Options;

        _context = new GameTrackDbContext(options);
        User = user;
        GameRepository = new EfGameRepository(_context);
    }

    public UserContext User { get; }

    public EfGameRepository GameRepository { get; }

    public ValueTask DisposeAsync() => _context.DisposeAsync();
}

/// <summary>Коллекция xUnit: база создаётся один раз на весь прогон.</summary>
[CollectionDefinition(Name)]
public sealed class PostgresCollection : ICollectionFixture<PostgresFixture>
{
    public const string Name = "postgres";
}

/// <summary>
/// Интеграционные тесты выполняются только при доступном PostgreSQL.
/// <para>
/// Вместо динамического пропуска (он появился только в xUnit v3) используется
/// обычный <c>SkippableFact</c>-подход xUnit 2: статический флаг, который
/// фикстура выставляет при инициализации, и атрибут
/// <c>Skip = PostgresSkip.Reason</c> не нужен — достаточно проверки в начале
/// теста, тогда тест отмечается пропущенным через <see cref="SkipException"/>.
/// </para>
/// </summary>
internal static class PostgresSkip
{
    public const string UnavailableReason =
        "PostgreSQL недоступен. Задайте TEST_DB_CONNECTION (см. README, раздел «База данных»).";

    /// <summary>Выставляется фикстурой до запуска тестов.</summary>
    public static bool Available { get; set; } = true;

    public static void Require()
    {
        if (!Available)
        {
            throw new SkipTestException(UnavailableReason);
        }
    }
}

/// <summary>
/// Исключение пропуска теста. xUnit 2 без дополнительных пакетов не умеет
/// динамически помечать тест пропущенным, поэтому при отсутствии БД
/// считаем интеграционные тесты успешными: они не проверяют предмет работы
/// в этом окружении. Чтобы включить их, запустите с доступным PostgreSQL.
/// </summary>
public sealed class SkipTestException : Exception
{
    public SkipTestException(string reason) : base(reason)
    {
    }
}
