using GameTrack.Domain.Repositories;
using GameTrack.Infrastructure.Persistence;
using GameTrack.Infrastructure.Security;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;

namespace GameTrack.Infrastructure;

/// <summary>
/// Регистрация хранилища данных. Единственное место, где Infrastructure
/// знает про PostgreSQL: строка подключения берётся из конфигурации
/// (UserSecrets в разработке), в appsettings.json остаётся только шаблон.
/// </summary>
public static class DependencyInjection
{
    /// <summary>Имя ключа строки подключения.</summary>
    public const string ConnectionStringName = "GameTrack";

    /// <summary>
    /// Подключает PostgreSQL: общий <see cref="NpgsqlDataSource"/> (singleton),
    /// scoped <see cref="GameTrackDbContext"/> в режиме database first
    /// (без миграций и EnsureCreated) и все репозитории.
    /// </summary>
    public static IServiceCollection AddPersistence(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        var connectionString = configuration.GetConnectionString(ConnectionStringName);
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException(
                $"Строка подключения 'ConnectionStrings:{ConnectionStringName}' не задана. " +
                "Задайте её через dotnet user-secrets (см. README, раздел «База данных»).");
        }

        return services.AddPersistence(connectionString);
    }

    /// <summary>Перегрузка для тестов и явной конфигурации.</summary>
    public static IServiceCollection AddPersistence(
        this IServiceCollection services,
        string connectionString)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionString);

        // Один пул соединений на приложение: SQL-репозитории и EF используют
        // один и тот же NpgsqlDataSource.
        services.AddSingleton(_ => NpgsqlDataSource.Create(connectionString));

        services.AddDbContext<GameTrackDbContext>((serviceProvider, options) =>
        {
            var dataSource = serviceProvider.GetRequiredService<NpgsqlDataSource>();
            options.UseNpgsql(dataSource);
        });

        // IGameRepository -> EF Core, остальные -> чистый SQL (Npgsql).
        services.AddScoped<IGameRepository, EfGameRepository>();
        services.AddScoped<IAccountRepository, NpgsqlAccountRepository>();
        services.AddScoped<IUserProfileRepository, NpgsqlUserProfileRepository>();

        // Конкретная реализация нужна middleware, который определяет
        // пользователя запроса (поиск логина по публичному Guid).
        services.AddScoped<NpgsqlAccountRepository>(serviceProvider =>
            (NpgsqlAccountRepository)serviceProvider.GetRequiredService<IAccountRepository>());

        return services;
    }
}
