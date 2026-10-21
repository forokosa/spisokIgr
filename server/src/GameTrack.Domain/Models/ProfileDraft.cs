namespace GameTrack.Domain.Models;

/// <summary>Данные, доступные для редактирования в профиле.</summary>
public sealed record ProfileDraft(string Name, string Email, string? AvatarUrl = null);
