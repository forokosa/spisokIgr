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
