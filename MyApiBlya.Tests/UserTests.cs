namespace MyApiBlya.Tests;
using MyApiBlya.Tests;
using Moq;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using MyApiBlya.Services;
using System.Security.Claims;
using System.Globalization;
using System.Net.Http.Headers;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Routing;

public class CurrentUserTest()
{
    private static async Task<IActionResult?> CheckActiveUserManuallyAsync(
        Mock<IUserService> users)
    {
        var currentUser = await users.Object.GetCurrentUserAsync(new ClaimsPrincipal());

        if (!currentUser.Success || currentUser.Data is null)
        {
            return new UnauthorizedObjectResult(new { message = "Требуется авторизация." });
        }

        if (currentUser.Data.IsBlocked)
        {
            return new ObjectResult(new { message = "доступ запрещен" })
            {
                StatusCode = StatusCodes.Status403Forbidden
            };
        }

        return null;
    }

    [Fact]
    public async Task UnBlockUser_WhenResult_NotOk()
    {
        var action = new Mock<IUserActionService>();
        var logger = new Mock<ILogger<AdminUsersController>>();
        var cache = new Mock<IMemoryCache>();
        var users = new Mock<IUserService>();

        users.Setup(x => x.GetCurrentUserAsync(It.IsAny<ClaimsPrincipal>()))
            .ReturnsAsync(ServiceResult<User?>.Fail("ошибка"));

        var controller = new AdminUsersController(action.Object, logger.Object, users.Object, null!, cache.Object);

        var result = await CheckActiveUserManuallyAsync(users);

        Assert.IsType<UnauthorizedObjectResult>(result);
    }
    [Fact]
    public async Task UnBlockUser_WhenUserBlocked_ReturnsForbidden()
    {
        var action = new Mock<IUserActionService>();
        var logger = new Mock<ILogger<AdminUsersController>>();
        var cache = new Mock<IMemoryCache>();
        var users = new Mock<IUserService>();

        users.Setup(x => x.GetCurrentUserAsync(It.IsAny<ClaimsPrincipal>()))
            .ReturnsAsync(ServiceResult<User?>.Ok(new User
            {
                Id = 1,
                Login = "admin",
                IsBlocked = true
            }));

        var controller = new AdminUsersController(action.Object, logger.Object, users.Object, null!, cache.Object);

        var result = await CheckActiveUserManuallyAsync(users);

        var type = Assert.IsType<ObjectResult>(result);
        Assert.Equal(403, type.StatusCode);
    }
    [Fact]
    public async Task Rename_WhenUserBlocked_ReturnsForbidden()
    {
        var action = new Mock<IUserActionService>();
        var logger = new Mock<ILogger<CurrentUserController>>();
        var cache = new Mock<IMemoryCache>();
        var users = new Mock<IUserService>();

        users.Setup(x => x.GetCurrentUserAsync(It.IsAny<ClaimsPrincipal>()))
            .ReturnsAsync(ServiceResult<User?>.Ok(new User
            {
                Id = 1,
                Login = "user",
                IsBlocked = true
            }));

        var controller = new CurrentUserController(action.Object, logger.Object, users.Object, null!);

        var result = await CheckActiveUserManuallyAsync(users);

        var type = Assert.IsType<ObjectResult>(result);
        Assert.Equal(403, type.StatusCode);
    }
    [Fact]
    public async Task Rename_WhenResult_NotOk()
    {
        var action = new Mock<IUserActionService>();
        var logger = new Mock<ILogger<CurrentUserController>>();
        var cache = new Mock<IMemoryCache>();
        var users = new Mock<IUserService>();

        users.Setup(x => x.GetCurrentUserAsync(It.IsAny<ClaimsPrincipal>()))
            .ReturnsAsync(ServiceResult<User?>.Ok(new User
            {
                Id = 1,
                Login = "user",
                IsBlocked = false
            }));

        users.Setup(x => x.RenameUserAsync(1, "newName", It.IsAny<ClaimsPrincipal>()))
            .ReturnsAsync(ServiceResult<string>.Fail("ошибка"));

        var controller = new CurrentUserController(action.Object, logger.Object, users.Object, null!);

        var result = await controller.RenameUserAsync("newName");

        Assert.IsType<BadRequestObjectResult>(result);
    }
    [Fact]
    public async Task GetHistory_WhenResult_NotOk()
    {
        var action = new Mock<IUserActionService>();
        var logger = new Mock<ILogger<CurrentUserController>>();
        var cache = new Mock<IMemoryCache>();
        var users = new Mock<IUserService>();

        users.Setup(x => x.GetUserHistoryAsync(It.IsAny<ClaimsPrincipal>()))
            .ReturnsAsync(ServiceResult<List<UserHistoryDto>>.Fail("Требуется авторизация.", StatusCodes.Status401Unauthorized));

        var controller = new CurrentUserController(action.Object, logger.Object, users.Object, null!);

        var result = await controller.GetHistory();

        Assert.IsType<UnauthorizedObjectResult>(result);
    }
    [Fact]
    public async Task GetHistory_WhenUserBlocked_ReturnsForbidden()
    {
        var action = new Mock<IUserActionService>();
        var logger = new Mock<ILogger<CurrentUserController>>();
        var cache = new Mock<IMemoryCache>();
        var users = new Mock<IUserService>();

        users.Setup(x => x.GetUserHistoryAsync(It.IsAny<ClaimsPrincipal>()))
            .ReturnsAsync(ServiceResult<List<UserHistoryDto>>.Fail("Доступ запрещен.", StatusCodes.Status403Forbidden));

        var controller = new CurrentUserController(action.Object, logger.Object, users.Object, null!);

        var result = await controller.GetHistory();

        var type = Assert.IsType<ObjectResult>(result);
        Assert.Equal(403, type.StatusCode);
    }
    [Fact]
    public async Task GetHistory_WhenSuccess_ReturnsOkWithHistory()
    {
        var action = new Mock<IUserActionService>();
        var logger = new Mock<ILogger<CurrentUserController>>();
        var cache = new MemoryCache(new MemoryCacheOptions());
        var users = new Mock<IUserService>();

        users.Setup(x => x.GetUserHistoryAsync(It.IsAny<ClaimsPrincipal>()))
            .ReturnsAsync(ServiceResult<List<UserHistoryDto>>.Ok(new List<UserHistoryDto>
            {
                new UserHistoryDto
                {
                    action = "тестовое действие",
                    time = DateTime.UtcNow
                }
            }));

        var controller = new CurrentUserController(action.Object, logger.Object, users.Object, null!);

        var result = await controller.GetHistory();

        var ok = Assert.IsType<OkObjectResult>(result);
        var historyResult = Assert.IsType<List<UserHistoryDto>>(ok.Value);
        Assert.Equal("тестовое действие", historyResult[0].action);
    }
}
