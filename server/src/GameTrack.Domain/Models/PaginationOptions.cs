namespace GameTrack.Domain.Models;

/// <summary>Настройки пагинации, приходящие из appsettings.json.</summary>
public sealed class PaginationOptions
{
    public const int HardMaxPageSize = 100;

    public int DefaultPageSize { get; set; } = 6;

    public int MaxPageSize { get; set; } = HardMaxPageSize;

    /// <summary>Проверяет корректность конфигурации.</summary>
    public void EnsureValid()
    {
        if (DefaultPageSize < 1 || DefaultPageSize > HardMaxPageSize)
        {
            throw new ArgumentOutOfRangeException(
                nameof(DefaultPageSize),
                $"Размер страницы по умолчанию должен быть от 1 до {HardMaxPageSize}.");
        }

        if (MaxPageSize < 1 || MaxPageSize > HardMaxPageSize)
        {
            throw new ArgumentOutOfRangeException(
                nameof(MaxPageSize),
                $"Максимальный размер страницы должен быть от 1 до {HardMaxPageSize}.");
        }

        if (DefaultPageSize > MaxPageSize)
        {
            throw new ArgumentException("Размер страницы по умолчанию не может превышать максимальный.");
        }
    }
}
