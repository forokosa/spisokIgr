using GameTrack.Domain.Enums;

namespace GameTrack.Domain.Models;

/// <summary>Данные для создания или полного обновления игры.</summary>
public sealed record GameDraft(
    string Title,
    string Genre,
    string Platform,
    GameStatus Status,
    string? CoverUrl = null,
    string? Description = null,
    decimal? Rating = null,
    int? ReleaseYear = null);
