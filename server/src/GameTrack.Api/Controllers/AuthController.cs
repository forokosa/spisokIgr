using GameTrack.Api.Contracts;
using GameTrack.Domain.Common;
using GameTrack.Domain.Models;
using GameTrack.Domain.Services;
using Microsoft.AspNetCore.Mvc;

namespace GameTrack.Api.Controllers;

/// <summary>Аутентификация и восстановление доступа.</summary>
[ApiController]
[Route("api/auth")]
[Produces("application/json")]
public sealed class AuthController : ControllerBase
{
    private readonly AuthService _auth;

    public AuthController(AuthService auth)
    {
        _auth = auth ?? throw new ArgumentNullException(nameof(auth));
    }

    /// <summary>Выполняет вход по email и паролю.</summary>
    [HttpPost("login")]
    [ProducesResponseType(typeof(AuthSessionResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<AuthSessionResponse>> Login(
        [FromBody] LoginRequest request,
        CancellationToken cancellationToken = default)
    {
        if (request is null)
        {
            return BadRequest(new ApiErrorResponse("Тело запроса обязательно."));
        }

        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        try
        {
            var session = await _auth.LoginAsync(
                request.Email,
                request.Password,
                request.Remember,
                cancellationToken);

            return Ok(AuthSessionResponse.FromDomain(session));
        }
        catch (UnauthorizedException exception)
        {
            return Unauthorized(new ApiErrorResponse(exception.Message));
        }
        catch (NotFoundException exception)
        {
            return NotFound(new ApiErrorResponse(exception.Message));
        }
        catch (ArgumentException exception)
        {
            return BadRequest(new ApiErrorResponse(exception.Message));
        }
    }

    /// <summary>Создаёт новый аккаунт и возвращает сессию.</summary>
    [HttpPost("register")]
    [ProducesResponseType(typeof(AuthSessionResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<AuthSessionResponse>> Register(
        [FromBody] RegisterRequest request,
        CancellationToken cancellationToken = default)
    {
        if (request is null)
        {
            return BadRequest(new ApiErrorResponse("Тело запроса обязательно."));
        }

        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        try
        {
            var session = await _auth.RegisterAsync(
                new RegisterDraft(
                    request.Name!,
                    request.Email!,
                    request.Password!,
                    request.Remember),
                cancellationToken);

            return StatusCode(
                StatusCodes.Status201Created,
                AuthSessionResponse.FromDomain(session));
        }
        catch (ConflictException exception)
        {
            return Conflict(new ApiErrorResponse(exception.Message));
        }
        catch (ArgumentException exception)
        {
            return BadRequest(new ApiErrorResponse(exception.Message));
        }
    }

    /// <summary>Запрашивает код восстановления пароля.</summary>
    [HttpPost("recovery")]
    [ProducesResponseType(typeof(RecoveryResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<RecoveryResponse>> RequestRecovery(
        [FromBody] RecoveryRequest request,
        CancellationToken cancellationToken = default)
    {
        if (request is null)
        {
            return BadRequest(new ApiErrorResponse("Тело запроса обязательно."));
        }

        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        try
        {
            var result = await _auth.RequestRecoveryAsync(request.Email, cancellationToken);
            return Ok(RecoveryResponse.FromDomain(result));
        }
        catch (NotFoundException exception)
        {
            return NotFound(new ApiErrorResponse(exception.Message));
        }
        catch (ArgumentException exception)
        {
            return BadRequest(new ApiErrorResponse(exception.Message));
        }
    }
}
