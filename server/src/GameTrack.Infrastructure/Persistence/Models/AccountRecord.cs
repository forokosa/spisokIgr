namespace GameTrack.Infrastructure.Persistence.Models;

/// <summary>
/// Минимальное представление accounts для EF-части: нужен только технический
/// ключ по логину. Остальные столбцы аккаунта читает SQL-репозиторий,
/// поэтому здесь их нет — EF не должен читать секреты.
/// </summary>
public sealed class AccountRecord
{
    public long Id { get; set; }

    public string Login { get; set; } = string.Empty;
}
