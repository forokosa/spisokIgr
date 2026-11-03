using GameTrack.Infrastructure.Persistence.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GameTrack.Infrastructure.Persistence.Configurations;

/// <summary>Маппинг связующей таблицы game_platforms.</summary>
public sealed class GamePlatformRecordConfiguration : IEntityTypeConfiguration<GamePlatformRecord>
{
    public void Configure(EntityTypeBuilder<GamePlatformRecord> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("game_platforms");

        builder.HasKey(link => new { link.GameId, link.PlatformId });

        builder.Property(link => link.GameId)
            .HasColumnName("game_id")
            .IsRequired();

        builder.Property(link => link.PlatformId)
            .HasColumnName("platform_id")
            .IsRequired();

        builder.HasOne(link => link.Game)
            .WithMany(game => game.GamePlatforms)
            .HasForeignKey(link => link.GameId)
            .HasConstraintName("game_platforms_game_id_fkey")
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(link => link.Platform)
            .WithMany(platform => platform.GamePlatforms)
            .HasForeignKey(link => link.PlatformId)
            .HasConstraintName("game_platforms_platform_id_fkey")
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(link => link.PlatformId)
            .HasDatabaseName("ix_game_platforms_platform");
    }
}
