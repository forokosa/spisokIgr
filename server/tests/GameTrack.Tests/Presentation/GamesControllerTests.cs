using GameTrack.Api.Contracts;
using GameTrack.Api.Controllers;
using GameTrack.Domain.Models;
using GameTrack.Domain.Services;
using GameTrack.Tests.Fakes;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace GameTrack.Tests.Presentation;

public sealed class GamesControllerTests
{
    private static GamesController CreateController()
    {
        var options = new PaginationOptions { DefaultPageSize = 6, MaxPageSize = 100 };
        var user = new FakeUserContextProvider(TestSeed.ProfileId, TestSeed.Login);
        var repository = new FakeGameRepository(TestSeed.ProfileId, TestSeed.CreateGames());
        var service = new GameService(repository, user, options);
        return new GamesController(service, options);
    }

    private static SaveGameRequest ValidRequest(string title = "Controller Game") => new()
    {
        Title = title,
        Genre = "RPG",
        Platform = "PC",
        Status = "want-to-play",
        Rating = 8m,
        ReleaseYear = 2020
    };

    private static async Task<Guid> AnyGameIdAsync(GamesController controller)
    {
        var page = await controller.GetGames(page: 1, pageSize: 1);
        return ((PagedResponse<GameResponse>)((OkObjectResult)page.Result!).Value!).Items[0].Id;
    }

    [Fact]
    public async Task GetGames_WithValidQuery_ReturnsOkWithPagedData()
    {
        var controller = CreateController();

        var result = await controller.GetGames(page: 1, pageSize: 6);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var payload = Assert.IsType<PagedResponse<GameResponse>>(ok.Value);
        Assert.Equal(6, payload.Items.Count);
        Assert.Equal(7, payload.TotalCount);
        Assert.Equal(2, payload.TotalPages);
        Assert.Equal(StatusCodes.Status200OK, ok.StatusCode);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task GetGames_WithInvalidPage_ReturnsBadRequest(int page)
    {
        var controller = CreateController();

        var result = await controller.GetGames(page: page);

        var badRequest = Assert.IsType<BadRequestObjectResult>(result.Result);
        Assert.Equal(StatusCodes.Status400BadRequest, badRequest.StatusCode);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(101)]
    public async Task GetGames_WithInvalidPageSize_ReturnsBadRequest(int pageSize)
    {
        var controller = CreateController();

        var result = await controller.GetGames(page: 1, pageSize: pageSize);

        Assert.IsType<BadRequestObjectResult>(result.Result);
    }

    [Fact]
    public async Task GetGames_WithTooLongSearch_ReturnsBadRequest()
    {
        var controller = CreateController();

        var result = await controller.GetGames(page: 1, search: new string('x', 101));

        Assert.IsType<BadRequestObjectResult>(result.Result);
    }

    [Fact]
    public async Task GetGames_WithInvalidStatus_ReturnsBadRequest()
    {
        var controller = CreateController();

        var result = await controller.GetGames(page: 1, status: "broken");

        Assert.IsType<BadRequestObjectResult>(result.Result);
    }

    [Fact]
    public async Task GetGameById_WithExistingId_ReturnsOk()
    {
        var controller = CreateController();
        var id = await AnyGameIdAsync(controller);

        var result = await controller.GetGameById(id.ToString());

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var game = Assert.IsType<GameResponse>(ok.Value);
        Assert.Equal(id, game.Id);
    }

    [Fact]
    public async Task GetGameById_WithInvalidId_ReturnsBadRequest()
    {
        var controller = CreateController();

        var result = await controller.GetGameById("not-a-guid");

        Assert.IsType<BadRequestObjectResult>(result.Result);
    }

    [Fact]
    public async Task GetGameById_WithUnknownId_ReturnsNotFound()
    {
        var controller = CreateController();

        var result = await controller.GetGameById(Guid.NewGuid().ToString());

        var notFound = Assert.IsType<NotFoundObjectResult>(result.Result);
        Assert.Equal(StatusCodes.Status404NotFound, notFound.StatusCode);
    }

    [Fact]
    public async Task CreateGame_WithValidRequest_ReturnsCreated()
    {
        var controller = CreateController();

        var result = await controller.CreateGame(ValidRequest());

        var created = Assert.IsType<CreatedAtActionResult>(result.Result);
        var game = Assert.IsType<GameResponse>(created.Value);
        Assert.Equal("Controller Game", game.Title);
        Assert.Equal(StatusCodes.Status201Created, created.StatusCode);
        Assert.Equal(nameof(GamesController.GetGameById), created.ActionName);
    }

    [Fact]
    public async Task CreateGame_WithInvalidTitle_ReturnsBadRequest()
    {
        var controller = CreateController();

        var result = await controller.CreateGame(ValidRequest(" "));

        var badRequest = Assert.IsType<BadRequestObjectResult>(result.Result);
        Assert.Equal(StatusCodes.Status400BadRequest, badRequest.StatusCode);
    }

    [Fact]
    public async Task CreateGame_WithDuplicateTitle_ReturnsConflict()
    {
        var controller = CreateController();

        var result = await controller.CreateGame(ValidRequest("The Witcher 3: Wild Hunt"));

        var conflict = Assert.IsType<ConflictObjectResult>(result.Result);
        Assert.Equal(StatusCodes.Status409Conflict, conflict.StatusCode);
    }

    [Fact]
    public async Task CreateGame_WithInvalidModelState_ReturnsBadRequest()
    {
        var controller = CreateController();
        controller.ModelState.AddModelError(nameof(SaveGameRequest.Title), "Ошибка");

        var result = await controller.CreateGame(ValidRequest());

        Assert.IsType<BadRequestObjectResult>(result.Result);
    }

    [Fact]
    public async Task CreateGame_WithInvalidStatus_ReturnsBadRequest()
    {
        var controller = CreateController();
        var request = ValidRequest();
        request = new SaveGameRequest
        {
            Title = request.Title,
            Genre = request.Genre,
            Platform = request.Platform,
            Status = "invalid",
            Rating = request.Rating,
            ReleaseYear = request.ReleaseYear
        };

        var result = await controller.CreateGame(request);

        Assert.IsType<BadRequestObjectResult>(result.Result);
    }

    [Fact]
    public async Task UpdateGame_WithValidRequest_ReturnsOk()
    {
        var controller = CreateController();
        var id = await AnyGameIdAsync(controller);

        var result = await controller.UpdateGame(id.ToString(), ValidRequest("Updated Controller Game"));

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var game = Assert.IsType<GameResponse>(ok.Value);
        Assert.Equal("Updated Controller Game", game.Title);
    }

    [Fact]
    public async Task UpdateGame_WithUnknownId_ReturnsNotFound()
    {
        var controller = CreateController();

        var result = await controller.UpdateGame(Guid.NewGuid().ToString(), ValidRequest());

        Assert.IsType<NotFoundObjectResult>(result.Result);
    }

    [Fact]
    public async Task UpdateGame_WithInvalidId_ReturnsBadRequest()
    {
        var controller = CreateController();

        var result = await controller.UpdateGame("bad-id", ValidRequest());

        Assert.IsType<BadRequestObjectResult>(result.Result);
    }

    [Fact]
    public async Task ChangeStatus_WithValidData_ReturnsOk()
    {
        var controller = CreateController();
        var id = await AnyGameIdAsync(controller);

        var result = await controller.ChangeStatus(
            id.ToString(),
            new ChangeGameStatusRequest { Status = "dropped" });

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var game = Assert.IsType<GameResponse>(ok.Value);
        Assert.Equal("dropped", game.Status);
    }

    [Fact]
    public async Task ChangeStatus_WithInvalidStatus_ReturnsBadRequest()
    {
        var controller = CreateController();
        var id = await AnyGameIdAsync(controller);

        var result = await controller.ChangeStatus(
            id.ToString(),
            new ChangeGameStatusRequest { Status = "broken" });

        Assert.IsType<BadRequestObjectResult>(result.Result);
    }

    [Fact]
    public async Task ChangeStatus_WithUnknownId_ReturnsNotFound()
    {
        var controller = CreateController();

        var result = await controller.ChangeStatus(
            Guid.NewGuid().ToString(),
            new ChangeGameStatusRequest { Status = "completed" });

        Assert.IsType<NotFoundObjectResult>(result.Result);
    }

    [Fact]
    public async Task DeleteGame_WithExistingId_ReturnsNoContent()
    {
        var controller = CreateController();
        var id = await AnyGameIdAsync(controller);

        var result = await controller.DeleteGame(id.ToString());

        Assert.IsType<NoContentResult>(result);
    }

    [Fact]
    public async Task DeleteGame_WithUnknownId_ReturnsNotFound()
    {
        var controller = CreateController();

        var result = await controller.DeleteGame(Guid.NewGuid().ToString());

        Assert.IsType<NotFoundObjectResult>(result);
    }

    [Fact]
    public async Task DeleteGame_WithInvalidId_ReturnsBadRequest()
    {
        var controller = CreateController();

        var result = await controller.DeleteGame("bad-id");

        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task Constructor_WithNullDependencies_ThrowsArgumentNullException()
    {
        var options = new PaginationOptions();
        var user = new FakeUserContextProvider(TestSeed.ProfileId, TestSeed.Login);
        var repository = new FakeGameRepository(TestSeed.ProfileId, TestSeed.CreateGames());
        var service = new GameService(repository, user, options);

        await Assert.ThrowsAsync<ArgumentNullException>(
            () => Task.FromResult(new GamesController(null!, options)));
        await Assert.ThrowsAsync<ArgumentNullException>(
            () => Task.FromResult(new GamesController(service, null!)));
    }
}
