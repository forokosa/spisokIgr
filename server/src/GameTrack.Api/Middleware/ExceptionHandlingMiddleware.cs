using System.Diagnostics;
using GameTrack.Api.Contracts;
using GameTrack.Domain.Common;
using Microsoft.AspNetCore.Mvc;

namespace GameTrack.Api.Middleware;

/// <summary>
/// Глобальный перехват исключений: доменные ошибки превращаются в корректные
/// HTTP-статусы, непредвиденные — в 500 без утечки деталей наружу.
/// </summary>
public sealed class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
    {
        _next = next ?? throw new ArgumentNullException(nameof(next));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task InvokeAsync(HttpContext context)
    {
        if (context is null)
        {
            throw new ArgumentNullException(nameof(context));
        }

        try
        {
            await _next(context);
        }
        catch (Exception exception) when (!context.RequestAborted.IsCancellationRequested)
        {
            await HandleExceptionAsync(context, exception);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        var traceId = Activity.Current?.Id ?? context.TraceIdentifier;

        var (statusCode, message) = exception switch
        {
            ArgumentNullException argumentNull => (StatusCodes.Status400BadRequest, argumentNull.Message),
            ArgumentOutOfRangeException outOfRange => (StatusCodes.Status400BadRequest, outOfRange.Message),
            ArgumentException argument => (StatusCodes.Status400BadRequest, argument.Message),
            NotFoundException notFound => (StatusCodes.Status404NotFound, notFound.Message),
            ConflictException conflict => (StatusCodes.Status409Conflict, conflict.Message),
            UnauthorizedException unauthorized => (StatusCodes.Status401Unauthorized, unauthorized.Message),
            OperationCanceledException => (StatusCodesExtra.ClientClosedRequest, "Запрос отменён."),
            _ => (StatusCodes.Status500InternalServerError, "Внутренняя ошибка сервера.")
        };

        if (statusCode >= StatusCodes.Status500InternalServerError)
        {
            _logger.LogError(exception, "Необработанная ошибка. TraceId: {TraceId}", traceId);
        }
        else
        {
            _logger.LogWarning(exception, "Обработанная ошибка {StatusCode}. TraceId: {TraceId}", statusCode, traceId);
        }

        if (context.Response.HasStarted)
        {
            return;
        }

        context.Response.Clear();
        context.Response.StatusCode = statusCode;
        context.Response.ContentType = "application/problem+json";

        var problem = new ProblemDetails
        {
            Status = statusCode,
            Title = statusCode >= 500 ? "Internal Server Error" : "Request Error",
            Detail = message,
            Instance = context.Request.Path,
            Extensions = { ["traceId"] = traceId }
        };

        await context.Response.WriteAsJsonAsync(problem);
    }
}

/// <summary>Стандартное расширение для подключения middleware.</summary>
public static class ExceptionHandlingMiddlewareExtensions
{
    public static IApplicationBuilder UseExceptionHandling(this IApplicationBuilder app)
        => app.UseMiddleware<ExceptionHandlingMiddleware>();
}

/// <summary>Статус 499 не входит в стандартный набор ASP.NET Core.</summary>
internal static class StatusCodesExtra
{
    public const int ClientClosedRequest = 499;
}
