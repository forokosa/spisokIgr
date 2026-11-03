using GameTrack.Infrastructure.Persistence.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GameTrack.Infrastructure.Persistence.Configurations;

/// <summary>Маппинг genres.</summary>
public sealed class GenreRecordConfiguration : IEntityTypeConfiguration<GenreRecord>
{
    public void Configure(EntityTypeBuilder<GenreRecord> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("genres");

        builder.HasKey(genre => genre.Id);

        builder.Property(genre => genre.Id)
            .HasColumnName("id")
            .ValueGeneratedOnAdd();

        builder.Property(genre => genre.Name)
            .HasColumnName("name")
            .HasMaxLength(50)
            .IsRequired();

        builder.HasIndex(genre => genre.Name)
            .HasDatabaseName("genres_name_key")
            .IsUnique();
    }
}
