using GameTrack.Domain.Models;

namespace GameTrack.Domain.Repositories;

/// <summary>
/// Абстракция хранилища учётных записей. Реализация отвечает за безопасное
/// хранение паролей (хеширование) и проверку учётных данных.
/// </summary>
public interface IAccountRepository
{
    Task<bool> ExistsAsync(string email, CancellationToken cancellationToken = default);

    Task<UserAccount?> FindAsync(string email, CancellationToken cancellationToken = default);

    /// <summary>
    /// Создаёт аккаунт. Логин для БД выводится из имени и email
    /// (прежнее API логина не передавало). При нарушении уникальности
    /// email или логина реализация обязана бросать <see cref="Common.ConflictException"/>.
    /// </summary>
    Task<UserAccount> RegisterAsync(
        string name,
        string email,
        string password,
        CancellationToken cancellationToken = default);

    Task<bool> ValidateAsync(
        string email,
        string password,
        CancellationToken cancellationToken = default);
}
