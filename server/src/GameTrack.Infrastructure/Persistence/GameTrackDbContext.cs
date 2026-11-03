using GameTrack.Infrastructure.Persistence.Models;
using Microsoft.EntityFrameworkCore;

namespace GameTrack.Infrastructure.Persistence;

/// <summary>
/// EF Core-контекст для доступа к играм, жанрам и платформам.
///
/// Режим "database first": схема (db/001_create_schema.sql) — источник истины,
/// поэтому здесь НЕТ миграций и вызовов EnsureCreated/Migrate. Конфигурации
/// в <c>Configurations</c> полностью повторяют существующие таблицы и задают
/// имена в snake_case явно, а не через глобальное преобразование, чтобы
/// маппинг можно было проверить глазами.
/// </summary>
public sealed class GameTrackDbContext : DbContext
{
    public GameTrackDbContext(DbContextOptions<GameTrackDbContext> options) : base(options)
    {
    }

    public DbSet<GameRecord> Games => Set<GameRecord>();

    public DbSet<AccountRecord> Accounts => Set<AccountRecord>();

    public DbSet<GameStatusRecord> GameStatuses => Set<GameStatusRecord>();

    public DbSet<GenreRecord> Genres => Set<GenreRecord>();

    public DbSet<PlatformRecord> Platforms => Set<PlatformRecord>();

    public DbSet<GameGenreRecord> GameGenres => Set<GameGenreRecord>();

    public DbSet<GamePlatformRecord> GamePlatforms => Set<GamePlatformRecord>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        ArgumentNullException.ThrowIfNull(modelBuilder);

        modelBuilder.ApplyConfigurationsFromAssembly(typeof(GameTrackDbContext).Assembly);

        base.OnModelCreating(modelBuilder);
    }
}
