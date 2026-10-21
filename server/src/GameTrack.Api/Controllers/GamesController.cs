using GameTrack.Api.Contracts;
using GameTrack.Domain.Common;
using GameTrack.Domain.Enums;
using GameTrack.Domain.Models;
using GameTrack.Domain.Services;
using Microsoft.AspNetCore.Mvc;

namespace GameTrack.Api.Controllers;

/// <summary>CRUD-операции над библиотекой игр.</summary>
[ApiController]
[Route("api/games")]
[Produces("application/json")]
public sealed class GamesController : ControllerBase
{
    private const int MaxSearchLength = 100;

    private readonly GameService _games;
    private readonly PaginationOptions _pagination;

    public GamesController(GameService games, PaginationOptions pagination)
    {
        _games = games ?? throw new ArgumentNullException(nameof(games));
        _pagination = pagination ?? throw new ArgumentNullException(nameof(pagination));
    }

    /// <summary>Возвращает страницу игр с поиском и фильтром по статусу.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(PagedResponse<GameResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<PagedResponse<GameResponse>>> GetGames(
        [FromQuery] int page = 1,
        [FromQuery] int? pageSize = null,
        [FromQuery] string? search = null,
        [FromQuery] string? status = null,
        CancellationToken cancellationToken = default)
    {
        if (page < 1)
        {
            return BadRequest(new ApiErrorResponse("Номер страницы должен быть больше или равен 1."));
        }

        var size = pageSize ?? _pagination.DefaultPageSize;
        if (size < 1 || size > _pagination.MaxPageSize)
        {
            return BadRequest(new ApiErrorResponse(
                $"Размер страницы должен быть от 1 до {_pagination.MaxPageSize}."));
        }

        if (search is not null && search.Trim().Length > MaxSearchLength)
        {
            return BadRequest(new ApiErrorResponse(
                $"Поисковый запрос не должен превышать {MaxSearchLength} символов."));
        }

        if (!string.IsNullOrWhiteSpace(status)
            && !status.Equals("all", StringComparison.OrdinalIgnoreCase)
            && !GameStatusExtensions.TryParse(status, out _))
        {
            return BadRequest(new ApiErrorResponse("Некорректный статус игры."));
        }

        try
        {
            var result = await _games.GetGamesAsync(page, size, search, status, cancellationToken);
            return Ok(new PagedResponse<GameResponse>(
                result.Items.Select(GameResponse.FromDomain).ToList(),
                result.Page,
                result.PageSize,
                result.TotalCount,
                result.TotalPages,
                result.HasPrevious,
                result.HasNext));
        }
        catch (ArgumentException exception)
        {
            return BadRequest(new ApiErrorResponse(exception.Message));
        }
    }

    /// <summary>Возвращает игру по идентификатору.</summary>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(GameResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<GameResponse>> GetGameById(
        string id,
        CancellationToken cancellationToken = default)
    {
        if (!Guid.TryParse(id, out var gameId) || gameId == Guid.Empty)
        {
            return BadRequest(new ApiErrorResponse("Некорректный идентификатор игры."));
        }

        try
        {
            var game = await _games.GetByIdAsync(gameId, cancellationToken);
            return Ok(GameResponse.FromDomain(game));
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

    /// <summary>Создаёт новую игру.</summary>
    [HttpPost]
    [ProducesResponseType(typeof(GameResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<GameResponse>> CreateGame(
        [FromBody] SaveGameRequest request,
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

        if (!GameStatusExtensions.TryParse(request.Status, out var status))
        {
            return BadRequest(new ApiErrorResponse("Некорректный статус игры."));
        }

        try
        {
            var draft = new GameDraft(
                request.Title!,
                request.Genre!,
                request.Platform!,
                status,
                request.CoverUrl,
                request.Description,
                request.Rating,
                request.ReleaseYear);

            var game = await _games.CreateAsync(draft, cancellationToken);
            var response = GameResponse.FromDomain(game);
            return CreatedAtAction(nameof(GetGameById), new { id = game.Id }, response);
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

    /// <summary>Полностью обновляет игру.</summary>
    [HttpPut("{id}")]
    [ProducesResponseType(typeof(GameResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<GameResponse>> UpdateGame(
        string id,
        [FromBody] SaveGameRequest request,
        CancellationToken cancellationToken = default)
    {
        if (!Guid.TryParse(id, out var gameId) || gameId == Guid.Empty)
        {
            return BadRequest(new ApiErrorResponse("Некорректный идентификатор игры."));
        }

        if (request is null)
        {
            return BadRequest(new ApiErrorResponse("Тело запроса обязательно."));
        }

        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        if (!GameStatusExtensions.TryParse(request.Status, out var status))
        {
            return BadRequest(new ApiErrorResponse("Некорректный статус игры."));
        }

        try
        {
            var draft = new GameDraft(
                request.Title!,
                request.Genre!,
                request.Platform!,
                status,
                request.CoverUrl,
                request.Description,
                request.Rating,
                request.ReleaseYear);

            var game = await _games.UpdateAsync(gameId, draft, cancellationToken);
            return Ok(GameResponse.FromDomain(game));
        }
        catch (NotFoundException exception)
        {
            return NotFound(new ApiErrorResponse(exception.Message));
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

    /// <summary>Меняет только статус игры.</summary>
    [HttpPatch("{id}/status")]
    [ProducesResponseType(typeof(GameResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<GameResponse>> ChangeStatus(
        string id,
        [FromBody] ChangeGameStatusRequest request,
        CancellationToken cancellationToken = default)
    {
        if (!Guid.TryParse(id, out var gameId) || gameId == Guid.Empty)
        {
            return BadRequest(new ApiErrorResponse("Некорректный идентификатор игры."));
        }

        if (request is null)
        {
            return BadRequest(new ApiErrorResponse("Тело запроса обязательно."));
        }

        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        if (!GameStatusExtensions.TryParse(request.Status, out _))
        {
            return BadRequest(new ApiErrorResponse("Некорректный статус игры."));
        }

        try
        {
            var game = await _games.ChangeStatusAsync(gameId, request.Status!, cancellationToken);
            return Ok(GameResponse.FromDomain(game));
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

    /// <summary>Удаляет игру.</summary>
    [HttpDelete("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteGame(
        string id,
        CancellationToken cancellationToken = default)
    {
        if (!Guid.TryParse(id, out var gameId) || gameId == Guid.Empty)
        {
            return BadRequest(new ApiErrorResponse("Некорректный идентификатор игры."));
        }

        try
        {
            await _games.DeleteAsync(gameId, cancellationToken);
            return NoContent();
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
