using GameTrack.Domain.Common;
using GameTrack.Domain.Repositories;
using GameTrack.Domain.Services;
using GameTrack.Infrastructure.Persistence;
using GameTrack.Infrastructure.Security;

namespace GameTrack.Api.Identity;

/// <summary>
/// Пользователь текущего HTTP-запроса. Значение вычисляется асинхронно
/// (нужен запрос к БД) и кладётся в <see cref="HttpContext.Items"/>
/// промежуточным ПО, поэтому доменные сервисы получают его синхронно.
/// </summary>
public sealed class HttpCurrentUser : IUserContextProvider
{
    public const string UserIdHeader = "X-User-Id";

    private readonly IHttpContextAccessor _accessor;

    public HttpCurrentUser(IHttpContextAccessor accessor)
    {
        ArgumentNullException.ThrowIfNull(accessor);
        _accessor = accessor;
    }

    /// <summary>
    /// Публичный идентификатор из заголовка X-User-Id; без заголовка —
    /// демо-профиль, как и в предыдущих версиях приложения.
    /// </summary>
    public static Guid ResolvePublicId(HttpContext? context)
    {
        var raw = context?.Request.Headers[UserIdHeader].FirstOrDefault();

        return Guid.TryParse(raw, out var userId) && userId != Guid.Empty
            ? userId
            : AccountIdMap.DemoUserId;
    }

    public UserContext GetRequiredUser()
    {
        var context = _accessor.HttpContext
                      ?? throw new InvalidOperationException("HTTP-контекст недоступен.");

        if (context.Items.TryGetValue(UserContextMiddleware.ItemKey, out var value)
            && value is UserContext user)
        {
            return user;
        }

        throw new NotFoundException("Пользователь не найден.");
    }
}

/// <summary>
/// Определяет <see cref="UserContext"/> один раз за запрос: берёт Guid из
/// X-User-Id, находит логин аккаунта в БД и проверяет, что обратное
/// отображение даёт тот же Guid.
///
/// Если аккаунт не найден, запрос не отклоняется на уровне middleware:
/// доменные сервисы вернут свои обычные ответы (401/404/пустой список),
/// поэтому контракты API для клиента не меняются.
/// </summary>
public sealed class UserContextMiddleware
{
    public const string ItemKey = "GameTrack.UserContext";

    public const string AccountMissingKey = "GameTrack.UserContextMissing";

    private readonly RequestDelegate _next;

    public UserContextMiddleware(RequestDelegate next)
    {
        _next = next ?? throw new ArgumentNullException(nameof(next));
    }

    public async Task InvokeAsync(
        HttpContext context,
        NpgsqlAccountRepository accounts,
        ILogger<UserContextMiddleware> logger)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(accounts);
        ArgumentNullException.ThrowIfNull(logger);

        var userId = HttpCurrentUser.ResolvePublicId(context);
        var login = await accounts.FindLoginByPublicIdAsync(userId, context.RequestAborted);

        if (login is null)
        {
            context.Items[AccountMissingKey] = userId;
            logger.LogInformation("Аккаунт {UserId} не найден в БД.", userId);
        }
        else
        {
            context.Items[ItemKey] = new UserContext(userId, login);
        }

        await _next(context);
    }
}

/// <summary>Подключение <see cref="UserContextMiddleware"/>.</summary>
public static class UserContextMiddlewareExtensions
{
    public static IApplicationBuilder UseCurrentUser(this IApplicationBuilder app)
        => app.UseMiddleware<UserContextMiddleware>();
}
