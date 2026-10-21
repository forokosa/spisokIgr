using GameTrack.Api.Contracts;
using GameTrack.Domain.Common;
using GameTrack.Domain.Models;
using GameTrack.Domain.Services;
using Microsoft.AspNetCore.Mvc;

namespace GameTrack.Api.Controllers;

/// <summary>Профиль пользователя.</summary>
[ApiController]
[Route("api/profile")]
[Produces("application/json")]
public sealed class ProfileController : ControllerBase
{
    private readonly ProfileService _profiles;

    public ProfileController(ProfileService profiles)
    {
        _profiles = profiles ?? throw new ArgumentNullException(nameof(profiles));
    }

    /// <summary>Возвращает профиль пользователя.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(ProfileResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ProfileResponse>> GetProfile(CancellationToken cancellationToken = default)
    {
        try
        {
            var profile = await _profiles.GetAsync(cancellationToken);
            return Ok(ProfileResponse.FromDomain(profile));
        }
        catch (NotFoundException exception)
        {
            return NotFound(new ApiErrorResponse(exception.Message));
        }
    }

    /// <summary>Обновляет профиль пользователя.</summary>
    [HttpPut]
    [ProducesResponseType(typeof(ProfileResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ProfileResponse>> UpdateProfile(
        [FromBody] UpdateProfileRequest request,
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
            var draft = new ProfileDraft(request.Name!, request.Email!, request.AvatarUrl);
            var profile = await _profiles.UpdateAsync(draft, cancellationToken);
            return Ok(ProfileResponse.FromDomain(profile));
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
