using System.Text.Json;
using GameTrack.Api.Middleware;
using GameTrack.Domain.Common;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;

namespace GameTrack.Tests.Presentation;

public sealed class ExceptionHandlingMiddlewareTests
{
    private static ExceptionHandlingMiddleware CreateMiddleware(RequestDelegate next)
        => new(next, NullLogger<ExceptionHandlingMiddleware>.Instance);

    private static DefaultHttpContext CreateContext()
    {
        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();
        return context;
    }

    [Fact]
    public async Task InvokeAsync_ArgumentException_ReturnsBadRequest()
    {
        var middleware = CreateMiddleware(_ => throw new ArgumentException("Плохой аргумент"));
        var context = CreateContext();

        await middleware.InvokeAsync(context);

        Assert.Equal(StatusCodes.Status400BadRequest, context.Response.StatusCode);
    }

    [Fact]
    public async Task InvokeAsync_ArgumentNullException_ReturnsBadRequest()
    {
        var middleware = CreateMiddleware(_ => throw new ArgumentNullException("value"));
        var context = CreateContext();

        await middleware.InvokeAsync(context);

        Assert.Equal(StatusCodes.Status400BadRequest, context.Response.StatusCode);
    }

    [Fact]
    public async Task InvokeAsync_NotFoundException_ReturnsNotFound()
    {
        var middleware = CreateMiddleware(_ => throw new NotFoundException("Не найдено"));
        var context = CreateContext();

        await middleware.InvokeAsync(context);

        Assert.Equal(StatusCodes.Status404NotFound, context.Response.StatusCode);
    }

    [Fact]
    public async Task InvokeAsync_ConflictException_ReturnsConflict()
    {
        var middleware = CreateMiddleware(_ => throw new ConflictException("Конфликт"));
        var context = CreateContext();

        await middleware.InvokeAsync(context);

        Assert.Equal(StatusCodes.Status409Conflict, context.Response.StatusCode);
    }

    [Fact]
    public async Task InvokeAsync_UnauthorizedException_ReturnsUnauthorized()
    {
        var middleware = CreateMiddleware(_ => throw new UnauthorizedException("Нет доступа"));
        var context = CreateContext();

        await middleware.InvokeAsync(context);

        Assert.Equal(StatusCodes.Status401Unauthorized, context.Response.StatusCode);
    }

    [Fact]
    public async Task InvokeAsync_UnexpectedException_ReturnsInternalServerError()
    {
        var middleware = CreateMiddleware(_ => throw new InvalidOperationException("Сбой"));
        var context = CreateContext();

        await middleware.InvokeAsync(context);

        Assert.Equal(StatusCodes.Status500InternalServerError, context.Response.StatusCode);
    }

    [Fact]
    public async Task InvokeAsync_WritesProblemDetails()
    {
        var middleware = CreateMiddleware(_ => throw new NotFoundException("Игра не найдена"));
        var context = CreateContext();

        await middleware.InvokeAsync(context);
        context.Response.Body.Position = 0;
        using var document = await JsonDocument.ParseAsync(context.Response.Body);

        Assert.Equal(404, document.RootElement.GetProperty("status").GetInt32());
        Assert.Equal("Игра не найдена", document.RootElement.GetProperty("detail").GetString());
    }

    [Fact]
    public async Task InvokeAsync_WithNullContext_ThrowsArgumentNullException()
    {
        var middleware = CreateMiddleware(_ => Task.CompletedTask);

        await Assert.ThrowsAsync<ArgumentNullException>(() => middleware.InvokeAsync(null!));
    }

    [Fact]
    public async Task InvokeAsync_WithSuccessfulRequest_KeepsStatusCode()
    {
        var middleware = CreateMiddleware(context =>
        {
            context.Response.StatusCode = StatusCodes.Status204NoContent;
            return Task.CompletedTask;
        });
        var context = CreateContext();

        await middleware.InvokeAsync(context);

        Assert.Equal(StatusCodes.Status204NoContent, context.Response.StatusCode);
    }
}
