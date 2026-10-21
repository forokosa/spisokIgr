using GameTrack.Infrastructure.Persistence;

namespace GameTrack.Api.Identity;

/// <summary>Идентификатор пользователя, от имени которого выполняется запрос.</summary>
public interface ICurrentUser
{
    Guid UserId { get; }
}

/// <summary>
/// Берёт идентификатор из заголовка X-User-Id. В демонстрационном приложении
/// полноценных токенов нет, поэтому при отсутствии заголовка используется демо-профиль.
/// </summary>
public sealed class HttpCurrentUser : ICurrentUser
{
    public HttpCurrentUser(IHttpContextAccessor accessor)
    {
        ArgumentNullException.ThrowIfNull(accessor);

        var raw = accessor.HttpContext?.Request.Headers["X-User-Id"].FirstOrDefault();
        UserId = Guid.TryParse(raw, out var userId) && userId != Guid.Empty
            ? userId
            : SeedData.ProfileId;
    }

    public Guid UserId { get; }
}
