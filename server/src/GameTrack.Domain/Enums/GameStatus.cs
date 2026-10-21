namespace GameTrack.Domain.Enums;

/// <summary>Статус игры в библиотеке пользователя.</summary>
public enum GameStatus
{
    InProgress = 1,
    Completed = 2,
    WantToPlay = 3,
    Dropped = 4
}

/// <summary>Строковые значения статуса, используемые в REST API и на клиенте.</summary>
public static class GameStatusExtensions
{
    public const string InProgressValue = "in-progress";
    public const string CompletedValue = "completed";
    public const string WantToPlayValue = "want-to-play";
    public const string DroppedValue = "dropped";

    public static string ToApiValue(this GameStatus status) => status switch
    {
        GameStatus.InProgress => InProgressValue,
        GameStatus.Completed => CompletedValue,
        GameStatus.WantToPlay => WantToPlayValue,
        GameStatus.Dropped => DroppedValue,
        _ => throw new ArgumentOutOfRangeException(nameof(status), "Неизвестный статус игры.")
    };

    public static bool TryParse(string? value, out GameStatus status)
    {
        switch (value?.Trim().ToLowerInvariant())
        {
            case InProgressValue:
                status = GameStatus.InProgress;
                return true;
            case CompletedValue:
                status = GameStatus.Completed;
                return true;
            case WantToPlayValue:
                status = GameStatus.WantToPlay;
                return true;
            case DroppedValue:
                status = GameStatus.Dropped;
                return true;
            default:
                status = default;
                return false;
        }
    }

    public static GameStatus Parse(string? value)
        => TryParse(value, out var status)
            ? status
            : throw new ArgumentException("Некорректный статус игры.", nameof(value));

    public static IReadOnlyList<GameStatus> All { get; } =
    [
        GameStatus.InProgress,
        GameStatus.Completed,
        GameStatus.WantToPlay,
        GameStatus.Dropped
    ];
}
