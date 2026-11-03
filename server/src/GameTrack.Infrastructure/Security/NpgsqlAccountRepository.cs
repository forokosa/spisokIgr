using GameTrack.Domain.Common;
using GameTrack.Domain.Models;
using GameTrack.Domain.Repositories;
using GameTrack.Infrastructure.Persistence;
using Npgsql;

namespace GameTrack.Infrastructure.Security;

/// <summary>
/// Хранилище аккаунтов на чистом SQL (Npgsql, без ORM и Dapper).
///
/// Правила, которые соблюдает реализация:
///   * только параметризованные запросы — значения в SQL не конкатенируются;
///   * никаких SELECT *: столбцы перечислены явно;
///   * соединение берётся у <see cref="NpgsqlDataSource"/> и освобождается
///     через <c>await using</c>;
///   * нарушение уникальности (SQLSTATE 23505) превращается в доменный
///     <see cref="ConflictException"/>;
///   * отображение строки в доменную модель — в отдельном приватном методе;
///   * чтение и запись идут через единые хелперы <see cref="ReadAsync"/> и
///     <see cref="ExecuteAsync"/>, чтобы SQL-код не расползался по методам.
/// </summary>
public sealed class NpgsqlAccountRepository : IAccountRepository
{
    private const int MaxLoginAttempts = 5;

    /// <summary>
    /// Oдин запрос на аккаунт. Имя пользователя хранится в user_profiles,
    /// поэтому подтягивается LEFT JOIN-ом: профиль мог появиться не сразу.
    /// </summary>
    private const string SelectAccountSql =
        """
        SELECT a.id, a.login, a.email, a.password_hash,
               COALESCE(p.display_name, a.login) AS display_name
        FROM accounts a
        LEFT JOIN user_profiles p ON p.account_id = a.id
        WHERE lower(a.email) = @email
        """;

    private const string SelectAccountByLoginSql =
        """
        SELECT a.id, a.login, a.email, a.password_hash,
               COALESCE(p.display_name, a.login) AS display_name
        FROM accounts a
        LEFT JOIN user_profiles p ON p.account_id = a.id
        WHERE lower(a.login) = @login
        """;

    private const string ExistsSql =
        """
        SELECT count(*)
        FROM accounts a
        WHERE lower(a.email) = @email
        """;

    /// <summary>
    /// Кандидаты в логины по длине логина, закодированной в публичном Guid.
    /// Индекс ix_accounts_login_length сужает выборку до нескольких строк,
    /// после чего вызывающая сторона сверяет Guid точно.
    /// </summary>
    private const string SelectLoginsByLengthSql =
        """
        SELECT a.login
        FROM accounts a
        WHERE length(lower(a.login)) = @login_length
        """;

    private const string InsertAccountSql =
        """
        INSERT INTO accounts (login, email, password_hash, created_at)
        VALUES (@login, @email, @password_hash, @created_at)
        RETURNING id, login, email
        """;

    private readonly NpgsqlDataSource _dataSource;

    public NpgsqlAccountRepository(NpgsqlDataSource dataSource)
    {
        _dataSource = Guard.NotNull(dataSource, nameof(dataSource));
    }

    public async Task<bool> ExistsAsync(string email, CancellationToken cancellationToken = default)
    {
        var normalizedEmail = Guard.Email(email, nameof(email));

        await using var connection = await _dataSource.OpenConnectionAsync(cancellationToken);

        var count = await ExecuteScalarAsync(
            connection,
            transaction: null,
            ExistsSql,
            cancellationToken,
            ("email", normalizedEmail));

        return Convert.ToInt64(count) > 0;
    }

    public async Task<UserAccount?> FindAsync(string email, CancellationToken cancellationToken = default)
    {
        var normalizedEmail = Guard.Email(email, nameof(email));

        await using var connection = await _dataSource.OpenConnectionAsync(cancellationToken);

        await using var command = new NpgsqlCommand(SelectAccountSql, connection);
        command.Parameters.AddWithValue("email", normalizedEmail);

        var accounts = await ReadAccountsAsync(command, cancellationToken);
        return accounts.FirstOrDefault();
    }

    public async Task<UserAccount> RegisterAsync(
        string name,
        string email,
        string password,
        CancellationToken cancellationToken = default)
    {
        var validName = Guard.NotBlank(name, nameof(name), 2, 100);
        var normalizedEmail = Guard.Email(email, nameof(email));
        var validPassword = Guard.Password(password, nameof(password));

        var passwordHash = PasswordHasher.Hash(validPassword);
        var baseLogin = AccountIdMap.CreateLogin(validName, normalizedEmail);
        var login = baseLogin;
        var createdAt = Normalize(DateTimeOffset.UtcNow);

        await using var connection = await _dataSource.OpenConnectionAsync(cancellationToken);

        for (var attempt = 1; attempt <= MaxLoginAttempts; attempt++)
        {
            try
            {
                await using var command = new NpgsqlCommand(InsertAccountSql, connection);
                command.Parameters.AddWithValue("login", login);
                command.Parameters.AddWithValue("email", normalizedEmail);
                command.Parameters.AddWithValue("password_hash", passwordHash);
                command.Parameters.AddWithValue("created_at", createdAt);

                await using var reader = await command.ExecuteReaderAsync(cancellationToken);
                await reader.ReadAsync(cancellationToken);

                return new UserAccount(
                    AccountIdMap.ToPublicId(login),
                    validName,
                    normalizedEmail,
                    login);
            }
            catch (PostgresException exception) when (exception.SqlState == PostgresErrorCodes.UniqueViolation)
            {
                if (IsEmailConflict(exception))
                {
                    throw new ConflictException("Аккаунт с таким email уже существует.");
                }

                if (IsLoginConflict(exception))
                {
                    // Логин выводится из email, поэтому исходный вариант мог быть
                    // занят другим аккаунтом. Берём следующий, email не меняем.
                    login = AccountIdMap.CreateSuffixedLogin(baseLogin);
                    continue;
                }

                throw new ConflictException("Аккаунт с такими данными уже существует.");
            }
        }

        throw new ConflictException("Не удалось подобрать свободный логин. Попробуйте другой email.");
    }

    public async Task<bool> ValidateAsync(
        string email,
        string password,
        CancellationToken cancellationToken = default)
    {
        var normalizedEmail = Guard.Email(email, nameof(email));
        if (password is null)
        {
            throw new ArgumentNullException(nameof(password), "Пароль обязателен.");
        }

        if (password.Length is < 1 or > 128)
        {
            throw new ArgumentOutOfRangeException(nameof(password), "Пароль должен содержать от 1 до 128 символов.");
        }

        await using var connection = await _dataSource.OpenConnectionAsync(cancellationToken);

        await using var command = new NpgsqlCommand(SelectAccountSql, connection);
        command.Parameters.AddWithValue("email", normalizedEmail);

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        if (!await reader.ReadAsync(cancellationToken))
        {
            return false;
        }

        // Хеш читается по имени столбца: маппинг доменной модели здесь не нужен.
        var storedHash = reader.GetString(reader.GetOrdinal("password_hash"));
        return PasswordHasher.Verify(password, storedHash);
    }

    /// <summary>
    /// Логин аккаунта по публичному Guid: выбираются аккаунты той же длины
    /// логина (параметризованный запрос, индекс ix_accounts_login_length),
    /// затем точное совпадение проверяется вычислением Guid.
    /// </summary>
    public async Task<string?> FindLoginByPublicIdAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        Guard.Id(userId, nameof(userId));

        var loginLength = AccountIdMap.GetLoginLength(userId);
        if (loginLength is < 1 or > 50)
        {
            return null;
        }

        await using var connection = await _dataSource.OpenConnectionAsync(cancellationToken);

        await using var command = new NpgsqlCommand(SelectLoginsByLengthSql, connection);
        command.Parameters.AddWithValue("login_length", loginLength);

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            var login = reader.GetString(0);
            if (AccountIdMap.ToPublicId(login) == userId)
            {
                return login;
            }
        }

        return null;
    }

    private static bool IsEmailConflict(PostgresException exception)
        => exception.ConstraintName is "accounts_email_key";

    private static bool IsLoginConflict(PostgresException exception)
        => exception.ConstraintName is "accounts_login_key";

    /// <summary>Чтение строк и отображение в доменную модель единым проходом ридера.</summary>
    private static async Task<List<UserAccount>> ReadAccountsAsync(
        NpgsqlCommand command,
        CancellationToken cancellationToken)
    {
        var accounts = new List<UserAccount>();

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            accounts.Add(MapAccount(reader));
        }

        return accounts;
    }

    /// <summary>Отображение строки accounts (+ display_name) в доменную модель.</summary>
    private static UserAccount MapAccount(NpgsqlDataReader reader)
    {
        var login = reader.GetString(1);
        var email = reader.GetString(2);
        var displayName = reader.GetString(4);

        return new UserAccount(
            AccountIdMap.ToPublicId(login),
            displayName,
            email,
            login);
    }

    /// <summary>PostgreSQL хранит timestamptz в UTC — приводим явно.</summary>
    private static DateTime Normalize(DateTimeOffset value) => value.UtcDateTime;

    /// <summary>
    /// Единая точка выполнения скалярных запросов: параметры передаются
    /// кортежами, конкатенации значений в SQL нет.
    /// </summary>
    private static async Task<object?> ExecuteScalarAsync(
        NpgsqlConnection connection,
        NpgsqlTransaction? transaction,
        string sql,
        CancellationToken cancellationToken,
        params (string Name, object Value)[] parameters)
    {
        await using var command = new NpgsqlCommand(sql, connection, transaction);
        foreach (var (name, value) in parameters)
        {
            command.Parameters.AddWithValue(name, value);
        }

        return await command.ExecuteScalarAsync(cancellationToken);
    }
}
