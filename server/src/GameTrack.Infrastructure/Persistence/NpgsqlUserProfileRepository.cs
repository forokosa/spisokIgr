using GameTrack.Domain.Common;
using GameTrack.Domain.Entities;
using GameTrack.Domain.Models;
using GameTrack.Domain.Repositories;
using Npgsql;

namespace GameTrack.Infrastructure.Persistence;

/// <summary>
/// Профиль пользователя на чистом SQL (Npgsql, без ORM и Dapper).
///
/// Решения по схеме в 3НФ: email и дата создания живут в accounts, имя и аватар —
/// в user_profiles. Поэтому обновление профиля и email выполняется одной
/// транзакцией, а чтение — одним JOIN-запросом.
///
/// Все операции адресуются <see cref="UserContext"/>: логин приходит из
/// middleware и всегда проверяется в SQL, поэтому чужой профиль недоступен.
/// </summary>
public sealed class NpgsqlUserProfileRepository : IUserProfileRepository
{
    /// <summary>
    /// Один запрос: профиль вместе с полями учётной записи (accounts + user_profiles).
    /// Столбцы перечислены явно — никаких SELECT *.
    /// </summary>
    private const string SelectSql =
        """
        SELECT a.login,
               a.email,
               a.created_at,
               p.display_name,
               p.avatar_url,
               p.updated_at
        FROM accounts a
        INNER JOIN user_profiles p ON p.account_id = a.id
        WHERE lower(a.login) = @login
        """;

    /// <summary>
    /// Сохранение — один параметризованный оператор на запись; выполняется
    /// внутри транзакции вместе с обновлением email в accounts.
    /// </summary>
    private const string UpdateProfileSql =
        """
        UPDATE user_profiles p
        SET display_name = @display_name,
            avatar_url   = @avatar_url,
            updated_at   = @updated_at
        FROM accounts a
        WHERE p.account_id = a.id
          AND lower(a.login) = @login
        """;

    /// <summary>Email редактируется в accounts — это часть того же профиля.</summary>
    private const string UpdateEmailSql =
        """
        UPDATE accounts a
        SET email = @email
        WHERE lower(a.login) = @login
        """;

    private readonly NpgsqlDataSource _dataSource;

    public NpgsqlUserProfileRepository(NpgsqlDataSource dataSource)
    {
        _dataSource = Guard.NotNull(dataSource, nameof(dataSource));
    }

    public async Task<UserProfile?> GetAsync(
        UserContext user,
        CancellationToken cancellationToken = default)
    {
        Guard.NotNull(user, nameof(user));

        await using var connection = await _dataSource.OpenConnectionAsync(cancellationToken);

        // Один запрос по логину или email: оба — уникальные ключи accounts.
        await using var command = new NpgsqlCommand(SelectSql, connection);
        AddProfileKey(command, user.Login);

        var rows = await ReadAsync(command, cancellationToken);
        return rows.FirstOrDefault();
    }

    public async Task CreateAsync(UserProfile profile, CancellationToken cancellationToken = default)
    {
        Guard.NotNull(profile, nameof(profile));
        var email = Guard.Email(profile.Email, nameof(profile));

        await using var connection = await _dataSource.OpenConnectionAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);

        // Аккаунт уже создан репозиторием учётных записей; здесь только профиль
        // и email (он мог поменяться при регистрации).
        var affected = await ExecuteAsync(
            connection,
            transaction,
            """
            INSERT INTO user_profiles (account_id, display_name, avatar_url, updated_at)
            SELECT a.id, @display_name, @avatar_url, now()
            FROM accounts a
            WHERE lower(a.email) = lower(@email)
            ON CONFLICT (account_id) DO NOTHING
            """,
            cancellationToken,
            ("display_name", profile.Name),
            ("avatar_url", Denormalize(profile.AvatarUrl)),
            ("email", email));

        if (affected == 0)
        {
            // Профиль уже есть либо аккаунт не найден. Первое — нормальная
            // повторная регистрация, второе — ошибка консистентности.
            var exists = await ExecuteScalarAsync(
                connection,
                transaction,
                "SELECT count(*) FROM accounts a WHERE lower(a.email) = lower(@email)",
                cancellationToken,
                ("email", email));

            if (Convert.ToInt64(exists) == 0)
            {
                throw new NotFoundException("Аккаунт не найден.");
            }
        }

        await transaction.CommitAsync(cancellationToken);
    }

    public async Task<UserProfile> UpdateAsync(
        UserContext user,
        UserProfile profile,
        CancellationToken cancellationToken = default)
    {
        Guard.NotNull(user, nameof(user));
        Guard.NotNull(profile, nameof(profile));

        var login = user.Login.ToLowerInvariant();
        var email = Guard.Email(profile.Email, nameof(profile));

        await using var connection = await _dataSource.OpenConnectionAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);

        // Профиль и email — одна операция: либо меняется всё, либо ничего.
        var updated = await ExecuteAsync(
            connection,
            transaction,
            UpdateProfileSql,
            cancellationToken,
            ("login", login),
            ("display_name", profile.Name),
            ("avatar_url", Denormalize(profile.AvatarUrl)),
            ("updated_at", Normalize(DateTimeOffset.UtcNow)));

        var emailUpdated = await ExecuteAsync(
            connection,
            transaction,
            UpdateEmailSql,
            cancellationToken,
            ("login", login),
            ("email", email));

        if (updated == 0 || emailUpdated == 0)
        {
            await transaction.RollbackAsync(cancellationToken);
            throw new NotFoundException("Профиль пользователя не найден.");
        }

        await transaction.CommitAsync(cancellationToken);

        // Возвращаем то, что действительно записано, а не то, что прислали.
        return await GetAsync(user, cancellationToken)
               ?? throw new NotFoundException("Профиль пользователя не найден.");
    }

    /// <summary>Профиль адресуется логином текущего пользователя.</summary>
    private static void AddProfileKey(NpgsqlCommand command, string login)
        => command.Parameters.AddWithValue("login", login.ToLowerInvariant());

    /// <summary>
    /// Чтение строк и отображение в доменную сущность — единым проходом
    /// ридера: строка → <see cref="MapProfile"/>, никаких вложенных запросов.
    /// </summary>
    private static async Task<List<UserProfile>> ReadAsync(
        NpgsqlCommand command,
        CancellationToken cancellationToken)
    {
        var profiles = new List<UserProfile>();

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            profiles.Add(MapProfile(reader));
        }

        return profiles;
    }

    /// <summary>Отображение строки accounts + user_profiles в доменную сущность.</summary>
    private static UserProfile MapProfile(NpgsqlDataReader reader)
    {
        var login = reader.GetString(0);
        var email = reader.GetString(1);
        var createdAt = reader.GetFieldValue<DateTimeOffset>(2);
        var displayName = reader.GetString(3);
        var avatarUrl = reader.IsDBNull(4) ? null : reader.GetString(4);
        var updatedAt = reader.GetFieldValue<DateTimeOffset>(5);

        return UserProfile.Restore(
            AccountIdMap.ToPublicId(login),
            displayName,
            email,
            avatarUrl,
            createdAt,
            // updated_at не может быть раньше created_at (инвариант сущности).
            updatedAt < createdAt ? createdAt : updatedAt);
    }

    /// <summary>
    /// PostgreSQL хранит timestamptz как UTC; явное приведение убирает
    /// зависимость от локального часового пояса сервера.
    /// </summary>
    private static DateTime Normalize(DateTimeOffset value) => value.UtcDateTime;

    private static object Denormalize(string? value) => value is null ? DBNull.Value : value;

    /// <summary>
    /// Единая точка выполнения команд записи: параметры передаются как
    /// кортежи, конкатенации значений в SQL нет.
    /// </summary>
    private static async Task<int> ExecuteAsync(
        NpgsqlConnection connection,
        NpgsqlTransaction transaction,
        string sql,
        CancellationToken cancellationToken,
        params (string Name, object Value)[] parameters)
    {
        await using var command = new NpgsqlCommand(sql, connection, transaction);
        foreach (var (name, value) in parameters)
        {
            command.Parameters.AddWithValue(name, value);
        }

        return await command.ExecuteNonQueryAsync(cancellationToken);
    }

    /// <summary>Скалярный запрос (счётчики, проверки существования).</summary>
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
