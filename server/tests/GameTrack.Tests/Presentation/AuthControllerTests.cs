using GameTrack.Api.Contracts;
using GameTrack.Api.Controllers;
using GameTrack.Domain.Services;
using GameTrack.Tests.Fakes;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace GameTrack.Tests.Presentation;

public sealed class AuthControllerTests
{
    private static AuthController CreateController()
    {
        var service = new AuthService(
            new FakeUserProfileRepository(),
            new FakeAccountRepository("demo@gametrack.app", "demo1234"));

        return new AuthController(service);
    }

    private static LoginRequest ValidLoginRequest(string email = "demo@gametrack.app", string password = "demo1234")
        => new() { Email = email, Password = password, Remember = true };

    private static RegisterRequest ValidRegisterRequest(
        string name = "Новый Игрок",
        string email = "new@gametrack.app",
        string password = "password123")
        => new() { Name = name, Email = email, Password = password, Remember = true };

    [Fact]
    public async Task Login_WithValidCredentials_ReturnsOk()
    {
        var controller = CreateController();

        var result = await controller.Login(ValidLoginRequest());

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var session = Assert.IsType<AuthSessionResponse>(ok.Value);
        Assert.Equal("demo@gametrack.app", session.Email);
        Assert.True(session.Remember);
    }

    [Fact]
    public async Task Login_WithWrongPassword_ReturnsUnauthorized()
    {
        var controller = CreateController();

        var result = await controller.Login(ValidLoginRequest(password: "wrong-password"));

        var unauthorized = Assert.IsType<UnauthorizedObjectResult>(result.Result);
        Assert.Equal(StatusCodes.Status401Unauthorized, unauthorized.StatusCode);
    }

    [Theory]
    [InlineData("not-an-email", "demo1234")]
    [InlineData("demo@gametrack.app", "123")]
    public async Task Login_WithInvalidData_ReturnsBadRequest(string email, string password)
    {
        var controller = CreateController();

        var result = await controller.Login(ValidLoginRequest(email, password));

        Assert.IsType<BadRequestObjectResult>(result.Result);
    }

    [Fact]
    public async Task Login_WithInvalidModelState_ReturnsBadRequest()
    {
        var controller = CreateController();
        controller.ModelState.AddModelError(nameof(LoginRequest.Email), "Ошибка");

        var result = await controller.Login(ValidLoginRequest());

        Assert.IsType<BadRequestObjectResult>(result.Result);
    }

    [Fact]
    public async Task Register_WithValidData_ReturnsCreatedSession()
    {
        var controller = CreateController();

        var result = await controller.Register(ValidRegisterRequest());

        var created = Assert.IsType<ObjectResult>(result.Result);
        Assert.Equal(StatusCodes.Status201Created, created.StatusCode);
        var session = Assert.IsType<AuthSessionResponse>(created.Value);
        Assert.Equal("new@gametrack.app", session.Email);
        Assert.Equal("Новый Игрок", session.Name);
        Assert.True(session.Remember);
    }

    [Fact]
    public async Task Register_WithExistingEmail_ReturnsConflict()
    {
        var controller = CreateController();

        var result = await controller.Register(
            ValidRegisterRequest(email: "demo@gametrack.app", password: "password123"));

        var conflict = Assert.IsType<ConflictObjectResult>(result.Result);
        Assert.Equal(StatusCodes.Status409Conflict, conflict.StatusCode);
    }

    [Theory]
    [InlineData("A", "new@gametrack.app", "password123")]
    [InlineData("Новый Игрок", "not-an-email", "password123")]
    [InlineData("Новый Игрок", "new@gametrack.app", "short")]
    [InlineData("Новый Игрок", "new@gametrack.app", "onlyletters")]
    public async Task Register_WithInvalidData_ReturnsBadRequest(
        string name,
        string email,
        string password)
    {
        var controller = CreateController();

        var result = await controller.Register(ValidRegisterRequest(name, email, password));

        Assert.IsType<BadRequestObjectResult>(result.Result);
    }

    [Fact]
    public async Task Register_WithInvalidModelState_ReturnsBadRequest()
    {
        var controller = CreateController();
        controller.ModelState.AddModelError(nameof(RegisterRequest.Email), "Ошибка");

        var result = await controller.Register(ValidRegisterRequest());

        Assert.IsType<BadRequestObjectResult>(result.Result);
    }

    [Fact]
    public async Task Register_WithNullBody_ReturnsBadRequest()
    {
        var controller = CreateController();

        var result = await controller.Register(null!);

        Assert.IsType<BadRequestObjectResult>(result.Result);
    }

    [Fact]
    public async Task RequestRecovery_WithValidEmail_ReturnsOk()
    {
        var controller = CreateController();

        var result = await controller.RequestRecovery(new RecoveryRequest { Email = "demo@gametrack.app" });

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var recovery = Assert.IsType<RecoveryResponse>(ok.Value);
        Assert.True(recovery.Delivered);
        Assert.Equal(6, recovery.Code.Length);
    }

    [Fact]
    public async Task RequestRecovery_WithUnknownEmail_ReturnsNotFound()
    {
        var controller = CreateController();

        var result = await controller.RequestRecovery(new RecoveryRequest { Email = "unknown@gametrack.app" });

        Assert.IsType<NotFoundObjectResult>(result.Result);
    }

    [Fact]
    public async Task RequestRecovery_WithInvalidEmail_ReturnsBadRequest()
    {
        var controller = CreateController();

        var result = await controller.RequestRecovery(new RecoveryRequest { Email = "bad" });

        Assert.IsType<BadRequestObjectResult>(result.Result);
    }

    [Fact]
    public async Task Constructor_WithNullService_ThrowsArgumentNullException()
    {
        await Assert.ThrowsAsync<ArgumentNullException>(
            () => Task.FromResult(new AuthController(null!)));
    }
}
