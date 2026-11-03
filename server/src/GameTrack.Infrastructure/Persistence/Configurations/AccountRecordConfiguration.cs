using GameTrack.Infrastructure.Persistence.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GameTrack.Infrastructure.Persistence.Configurations;

/// <summary>
/// Маппинг accounts. EF видит только ключ и логин: пароли и email читает
/// SQL-репозиторий аккаунтов, поэтому EF-модель их не содержит.
/// </summary>
public sealed class AccountRecordConfiguration : IEntityTypeConfiguration<AccountRecord>
{
    public void Configure(EntityTypeBuilder<AccountRecord> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("accounts");

        builder.HasKey(account => account.Id);

        builder.Property(account => account.Id)
            .HasColumnName("id");

        builder.Property(account => account.Login)
            .HasColumnName("login")
            .HasMaxLength(50)
            .IsRequired();
    }
}
