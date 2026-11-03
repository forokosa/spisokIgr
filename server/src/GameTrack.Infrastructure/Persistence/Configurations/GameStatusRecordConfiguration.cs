using GameTrack.Infrastructure.Persistence.Converters;
using GameTrack.Infrastructure.Persistence.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GameTrack.Infrastructure.Persistence.Configurations;

/// <summary>
/// Маппинг game_statuses. Столбец code отображается на enum <c>GameStatus</c>
/// через <see cref="GameStatusConverter"/>: статус в коде — это доменное
/// значение, а не номер строки справочника.
/// </summary>
public sealed class GameStatusRecordConfiguration : IEntityTypeConfiguration<GameStatusRecord>
{
    public void Configure(EntityTypeBuilder<GameStatusRecord> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("game_statuses");

        builder.HasKey(status => status.Id);

        builder.Property(status => status.Id)
            .HasColumnName("id")
            .ValueGeneratedNever();

        builder.Property(status => status.Code)
            .HasColumnName("code")
            .HasMaxLength(30)
            .HasConversion(GameStatusConverter.Instance)
            .IsRequired();

        builder.Property(status => status.Name)
            .HasColumnName("name")
            .HasMaxLength(50)
            .IsRequired();

        builder.HasIndex(status => status.Code)
            .HasDatabaseName("game_statuses_code_key")
            .IsUnique();
    }
}
