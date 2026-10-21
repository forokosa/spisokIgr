using GameTrack.Domain.Enums;

namespace GameTrack.Domain.Models;

/// <summary>Критерии поиска и фильтрации библиотеки.</summary>
public sealed record GameFilter(string? Search = null, GameStatus? Status = null);
