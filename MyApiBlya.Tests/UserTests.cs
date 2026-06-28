namespace MyApiBlya.Tests;
using MyApiBlya.Tests;
using Moq;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using MyApiBlya.Services;
using System.Security.Claims;
using System.Globalization;
using System.Net.Http.Headers;
using Microsoft.EntityFrameworkCore;

public class CurrentUserTest()
{
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

        var result = await controller.UnblockUser(1);

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

        var result = await controller.UnblockUser(2);

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

        var controller = new CurrentUserController(action.Object, logger.Object, users.Object, null!, cache.Object);

        var result = await controller.RenameUserAsync("newName");

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

        var controller = new CurrentUserController(action.Object, logger.Object, users.Object, null!, cache.Object);

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

        users.Setup(x => x.GetCurrentUserAsync(It.IsAny<ClaimsPrincipal>()))
            .ReturnsAsync(ServiceResult<User?>.Fail("ошибка"));

        var controller = new CurrentUserController(action.Object, logger.Object, users.Object, null!, cache.Object);

        var result = await controller.GetHistory();

        Assert.IsType<UnauthorizedResult>(result);
    }
    [Fact]
    public async Task GetHistory_WhenUserBlocked_ReturnsForbidden()
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

        var controller = new CurrentUserController(action.Object, logger.Object, users.Object, null!, cache.Object);

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

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        var context = new AppDbContext(options);
        var user = new User
        {
            Id = 1,
            Login = "user",
            IsBlocked = false
        };
        var UserAction = new UserAction
        {
            Id = 1,
            Action = "тестовое действие"
        };

        context.Users.Add(user);
        context.UserActions.Add(UserAction);
        context.UserActionHistories.Add(new UserActionHistory
        {
            users_id = user.Id,
            actions_id = UserAction.Id,
            CreatedAt = DateTime.UtcNow
        });
        await context.SaveChangesAsync();

        users.Setup(x => x.GetCurrentUserAsync(It.IsAny<ClaimsPrincipal>()))
            .ReturnsAsync(ServiceResult<User?>.Ok(user));

        var controller = new CurrentUserController(action.Object, logger.Object, users.Object, context, cache);

        var result = await controller.GetHistory();

        var ok = Assert.IsType<OkObjectResult>(result);
        var historyResult = Assert.IsType<List<UserHistoryDto>>(ok.Value);
        Assert.Equal("тестовое действие", historyResult[0].action);
    }
}



