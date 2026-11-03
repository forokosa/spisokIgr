using GameTrack.Infrastructure.Persistence.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GameTrack.Infrastructure.Persistence.Configurations;

/// <summary>Маппинг platforms.</summary>
public sealed class PlatformRecordConfiguration : IEntityTypeConfiguration<PlatformRecord>
{
    public void Configure(EntityTypeBuilder<PlatformRecord> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("platforms");

        builder.HasKey(platform => platform.Id);

        builder.Property(platform => platform.Id)
            .HasColumnName("id")
            .ValueGeneratedOnAdd();

        builder.Property(platform => platform.Name)
            .HasColumnName("name")
            .HasMaxLength(50)
            .IsRequired();

        builder.HasIndex(platform => platform.Name)
            .HasDatabaseName("platforms_name_key")
            .IsUnique();
    }
}
