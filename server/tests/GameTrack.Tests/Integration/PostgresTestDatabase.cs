using System.Text.RegularExpressions;
using Npgsql;

namespace GameTrack.Tests.Integration;

/// <summary>
/// Отдельная база для интеграционных тестов.
///
/// Выбор сделан в пользу отдельной тестовой БД, а не Testcontainers:
///   * на машине разработки уже есть PostgreSQL (как и в CI вуза), Docker
///     может отсутствовать — тогда тесты просто не запустились бы;
///   * скрипты db/*.sql — источник истины, и их можно прогнать на реальной
///     схеме ровно теми командами, что использует человек;
///   * тесты не трогают рабочую базу приложения: создаётся временная база
///     gametrack_tests_&lt;метка&gt; и удаляется после прогона.
///
/// Подключение задаётся строкой TEST_DB_CONNECTION (по умолчанию —
/// локальный postgres, как в README). Если сервер недоступен, тесты
/// пропускаются, а не падают на чужой машине.
/// </summary>
public sealed class PostgresTestDatabase
{
    public const string ConnectionStringVariable = "TEST_DB_CONNECTION";

    public const string DefaultConnectionString =
        "Host=localhost;Port=5432;Database=postgres;Username=postgres;Password=postgres";

    private readonly string _databaseName;

    private PostgresTestDatabase(string databaseName, string connectionString)
    {
        _databaseName = databaseName;
        ConnectionString = connectionString;
    }

    /// <summary>Строка подключения к временной базе.</summary>
    public string ConnectionString { get; }

    public string DatabaseName => _databaseName;

    /// <summary>Базовая строка (к серверу, без выбора конкретной базы).</summary>
    public static string AdminConnectionString =>
        Environment.GetEnvironmentVariable(ConnectionStringVariable) ?? DefaultConnectionString;

    public static async Task<bool> IsAvailableAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            await using var connection = new NpgsqlConnection(AdminConnectionString);
            await connection.OpenAsync(cancellationToken);
            await using var command = new NpgsqlCommand("SELECT 1", connection);
            _ = await command.ExecuteScalarAsync(cancellationToken);
            return true;
        }
        catch (Exception)
        {
            return false;
        }
    }

    /// <summary>Создаёт пустую базу и выполняет скрипты схемы и демо-данных.</summary>
    public static async Task<PostgresTestDatabase> CreateAsync(
        bool includeDemoData = true,
        CancellationToken cancellationToken = default)
    {
        var name = $"gametrack_tests_{Guid.NewGuid():N}"[..40];
        var builder = new NpgsqlConnectionStringBuilder(AdminConnectionString) { Database = name };
        var connectionString = builder.ConnectionString;

        await ExecuteAsync(
            AdminConnectionString,
            $"CREATE DATABASE \"{name}\" WITH ENCODING 'UTF8' TEMPLATE template0",
            cancellationToken);

        var database = new PostgresTestDatabase(name, connectionString);

        foreach (var script in ScriptFiles(includeDemoData))
        {
            await ExecuteScriptAsync(connectionString, script, cancellationToken);
        }

        return database;
    }

    /// <summary>Удаляет временную базу.</summary>
    public async Task DropAsync(CancellationToken cancellationToken = default)
    {
        NpgsqlConnection.ClearAllPools();

        await ExecuteAsync(
            AdminConnectionString,
            $"DROP DATABASE IF EXISTS \"{_databaseName}\" WITH (FORCE)",
            cancellationToken);
    }

    /// <summary>Находит папку db/ относительно проекта тестов.</summary>
    public static string FindScriptDirectory()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);

        while (directory is not null)
        {
            var candidate = Path.Combine(directory.FullName, "db");
            if (Directory.Exists(candidate) && File.Exists(Path.Combine(candidate, "001_create_schema.sql")))
            {
                return candidate;
            }

            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException("Не найдена папка db/ со скриптами схемы.");
    }

    private static IEnumerable<string> ScriptFiles(bool includeDemoData)
    {
        var directory = FindScriptDirectory();

        // Порядок важен: схема, справочники, (демо-данные).
        var files = new List<string>
        {
            Path.Combine(directory, "001_create_schema.sql"),
            Path.Combine(directory, "002_seed_reference_data.sql")
        };

        if (includeDemoData)
        {
            files.Add(Path.Combine(directory, "003_seed_demo_data.sql"));
        }

        return files;
    }

    private static async Task ExecuteScriptAsync(
        string connectionString,
        string path,
        CancellationToken cancellationToken)
    {
        var script = await File.ReadAllTextAsync(path, cancellationToken);

        // Скрипты написаны для psql: управляющие команды \gset и подобные
        // не исполняются сервером и в тестах не встречаются.
        script = Regex.Replace(script, @"^\s*\\.*$", string.Empty, RegexOptions.Multiline);

        await ExecuteAsync(connectionString, script, cancellationToken);
    }

    private static async Task ExecuteAsync(
        string connectionString,
        string sql,
        CancellationToken cancellationToken)
    {
        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync(cancellationToken);

        await using var command = new NpgsqlCommand(sql, connection);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }
}
