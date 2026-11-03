using GameTrack.Domain.Enums;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace GameTrack.Infrastructure.Persistence.Converters;

/// <summary>
/// Enum <see cref="GameStatus"/> &lt;-&gt; game_statuses.code.
///
/// В таблице games статус хранится ссылкой <c>status_id</c> (smallint),
/// а код статуса живёт в справочнике game_statuses.code. Сопоставляем
/// доменный enum с кодом справочника, а не с числом: так значение статуса
/// не зависит от нумерации строк справочника (она в разных БД разная)
/// и читается человеком:
///   InProgress -&gt; 'Playing'
///   Completed  -&gt; 'Completed'
///   WantToPlay -&gt; 'Planned'
///   Dropped    -&gt; 'Dropped'
/// Неизвестный код из БД даёт исключение, а не «тихое» значение по умолчанию.
/// </summary>
public static class GameStatusConverter
{
    public const string PlayingCode = "Playing";
    public const string CompletedCode = "Completed";
    public const string PlannedCode = "Planned";
    public const string DroppedCode = "Dropped";

    public static readonly ValueConverter<GameStatus, string> Instance = new(
        status => ToCode(status),
        code => FromCode(code));

    public static string ToCode(GameStatus status) => status switch
    {
        GameStatus.InProgress => PlayingCode,
        GameStatus.Completed => CompletedCode,
        GameStatus.WantToPlay => PlannedCode,
        GameStatus.Dropped => DroppedCode,
        _ => throw new ArgumentOutOfRangeException(nameof(status), status, "Неизвестный статус игры.")
    };

    public static GameStatus FromCode(string? code) => code switch
    {
        PlayingCode => GameStatus.InProgress,
        // Совместимость со старыми кодами справочника.
        "InProgress" => GameStatus.InProgress,
        CompletedCode => GameStatus.Completed,
        PlannedCode => GameStatus.WantToPlay,
        "WantToPlay" => GameStatus.WantToPlay,
        DroppedCode => GameStatus.Dropped,
        _ => throw new InvalidOperationException(
            $"В game_statuses найден неизвестный код статуса '{code}'. Проверьте справочник game_statuses.")
    };
}
