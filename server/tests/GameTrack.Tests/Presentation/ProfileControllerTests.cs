using GameTrack.Api.Contracts;
using GameTrack.Api.Controllers;
using GameTrack.Domain.Services;
using GameTrack.Tests.Fakes;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace GameTrack.Tests.Presentation;

public sealed class ProfileControllerTests
{
    private static ProfileController CreateController()
    {
        var service = new ProfileService(
            new FakeUserProfileRepository(TestSeed.ProfileId, TestSeed.CreateProfile()),
            new FakeUserContextProvider(TestSeed.ProfileId, TestSeed.Login));
        return new ProfileController(service);
    }

    [Fact]
    public async Task GetProfile_ReturnsOkWithProfile()
    {
        var controller = CreateController();

        var result = await controller.GetProfile();

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var profile = Assert.IsType<ProfileResponse>(ok.Value);
        Assert.Equal("demo@gametrack.app", profile.Email);
    }

    [Fact]
    public async Task UpdateProfile_WithValidData_ReturnsOk()
    {
        var controller = CreateController();

        var result = await controller.UpdateProfile(new UpdateProfileRequest
        {
            Name = "Новый Пользователь",
            Email = "new@example.com"
        });

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var profile = Assert.IsType<ProfileResponse>(ok.Value);
        Assert.Equal("Новый Пользователь", profile.Name);
        Assert.Equal("new@example.com", profile.Email);
    }

    [Fact]
    public async Task UpdateProfile_WithInvalidEmail_ReturnsBadRequest()
    {
        var controller = CreateController();

        var result = await controller.UpdateProfile(new UpdateProfileRequest
        {
            Name = "Новый Пользователь",
            Email = "not-an-email"
        });

        var badRequest = Assert.IsType<BadRequestObjectResult>(result.Result);
        Assert.Equal(StatusCodes.Status400BadRequest, badRequest.StatusCode);
    }

    [Fact]
    public async Task UpdateProfile_WithInvalidName_ReturnsBadRequest()
    {
        var controller = CreateController();

        var result = await controller.UpdateProfile(new UpdateProfileRequest
        {
            Name = "A",
            Email = "valid@example.com"
        });

        Assert.IsType<BadRequestObjectResult>(result.Result);
    }

    [Fact]
    public async Task UpdateProfile_WithInvalidModelState_ReturnsBadRequest()
    {
        var controller = CreateController();
        controller.ModelState.AddModelError(nameof(UpdateProfileRequest.Email), "Ошибка");

        var result = await controller.UpdateProfile(new UpdateProfileRequest
        {
            Name = "Valid Name",
            Email = "valid@example.com"
        });

        Assert.IsType<BadRequestObjectResult>(result.Result);
    }

    [Fact]
    public async Task Constructor_WithNullService_ThrowsArgumentNullException()
    {
        await Assert.ThrowsAsync<ArgumentNullException>(
            () => Task.FromResult(new ProfileController(null!)));
    }
}
