using GameTrack.Infrastructure.Persistence.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GameTrack.Infrastructure.Persistence.Configurations;

/// <summary>Маппинг games под существующую таблицу (database first).</summary>
public sealed class GameRecordConfiguration : IEntityTypeConfiguration<GameRecord>
{
    public void Configure(EntityTypeBuilder<GameRecord> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("games");

        builder.HasKey(game => game.Id);

        builder.Property(game => game.Id)
            .HasColumnName("id")
            .ValueGeneratedOnAdd();

        builder.Property(game => game.AccountId)
            .HasColumnName("account_id")
            .IsRequired();

        builder.Property(game => game.Title)
            .HasColumnName("title")
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(game => game.CoverUrl)
            .HasColumnName("cover_url")
            .HasMaxLength(500);

        builder.Property(game => game.Description)
            .HasColumnName("description");

        // games.status_id -> game_statuses.id: статус берётся из справочника
        // по внешнему ключу, а не хранится кодом в самой игре (3НФ).
        builder.Property(game => game.StatusId)
            .HasColumnName("status_id")
            .IsRequired();

        // rating numeric(3,1): домен допускает дробные оценки вроде 9.5.
        builder.Property(game => game.Rating)
            .HasColumnName("rating")
            .HasPrecision(3, 1);

        builder.Property(game => game.HoursPlayed)
            .HasColumnName("hours_played")
            .HasPrecision(7, 1)
            .IsRequired();

        builder.Property(game => game.ReleaseYear)
            .HasColumnName("release_year");

        builder.Property(game => game.CreatedAt)
            .HasColumnName("created_at")
            .IsRequired();

        builder.Property(game => game.UpdatedAt)
            .HasColumnName("updated_at")
            .IsRequired();

        builder.HasOne(game => game.Status)
            .WithMany()
            .HasForeignKey(game => game.StatusId)
            .HasConstraintName("games_status_id_fkey")
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(game => game.AccountId)
            .HasDatabaseName("ix_games_account_id");

        builder.HasIndex(game => game.StatusId)
            .HasDatabaseName("ix_games_status_id");

        builder.HasIndex(game => new { game.AccountId, game.Title })
            .HasDatabaseName("ix_games_account_title");
    }
}
