using GameTrack.Api.Contracts;
using GameTrack.Domain.Services;
using Microsoft.AspNetCore.Mvc;

namespace GameTrack.Api.Controllers;

/// <summary>Статистика библиотеки.</summary>
[ApiController]
[Route("api/statistics")]
[Produces("application/json")]
public sealed class StatisticsController : ControllerBase
{
    private readonly StatisticsService _statistics;

    public StatisticsController(StatisticsService statistics)
    {
        _statistics = statistics ?? throw new ArgumentNullException(nameof(statistics));
    }

    /// <summary>Возвращает агрегированную статистику библиотеки.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(StatisticsResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<StatisticsResponse>> GetStatistics(
        CancellationToken cancellationToken = default)
    {
        var statistics = await _statistics.GetStatisticsAsync(cancellationToken);
        return Ok(StatisticsResponse.FromDomain(statistics));
    }
}
