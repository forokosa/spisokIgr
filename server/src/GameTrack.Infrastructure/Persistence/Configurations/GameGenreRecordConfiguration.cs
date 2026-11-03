using GameTrack.Infrastructure.Persistence.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GameTrack.Infrastructure.Persistence.Configurations;

/// <summary>Маппинг связующей таблицы game_genres.</summary>
public sealed class GameGenreRecordConfiguration : IEntityTypeConfiguration<GameGenreRecord>
{
    public void Configure(EntityTypeBuilder<GameGenreRecord> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("game_genres");

        builder.HasKey(link => new { link.GameId, link.GenreId });

        builder.Property(link => link.GameId)
            .HasColumnName("game_id")
            .IsRequired();

        builder.Property(link => link.GenreId)
            .HasColumnName("genre_id")
            .IsRequired();

        builder.HasOne(link => link.Game)
            .WithMany(game => game.GameGenres)
            .HasForeignKey(link => link.GameId)
            .HasConstraintName("game_genres_game_id_fkey")
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(link => link.Genre)
            .WithMany(genre => genre.GameGenres)
            .HasForeignKey(link => link.GenreId)
            .HasConstraintName("game_genres_genre_id_fkey")
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(link => link.GenreId)
            .HasDatabaseName("ix_game_genres_genre_id");
    }
}
