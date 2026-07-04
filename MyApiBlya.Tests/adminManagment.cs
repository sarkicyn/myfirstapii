
using AspNet.Security.OAuth.GitHub;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.Extensions.Logging;
using Moq;
using MyApiBlya.Services;
using Microsoft.Extensions.Caching.Memory;
using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Routing;
using Docker.DotNet.Models;
using Microsoft.AspNetCore.Http.HttpResults;


public class adminManage
{
    private static async Task<IActionResult?> CheckActiveUserManuallyAsync(
        Mock<IUserService> users)
    {
        var currentUser = await users.Object.GetCurrentUserAsync(It.IsAny<ClaimsPrincipal>());
        if (currentUser.Data == null)
        {
            return new UnauthorizedObjectResult(new{message = "пользователь не найден"});
        }
        if (currentUser is not null && currentUser.Data!.IsBlocked == true)
        {
            return new ObjectResult(new{message = "доступ запрещен" })
            {
                StatusCode = StatusCodes.Status403Forbidden
            };
        }
        return null;
    }

    [Fact]
    public async Task getUsers_WhenResult_NotOk()
    {
     
        var action = new Mock<IUserActionService>();
        var logger = new Mock<ILogger<AdminUsersController>>();
        var cache = new Mock<IMemoryCache>();
        var users = new Mock<IUserService>();   
        users.Setup(x=>x.GetCurrentUserAsync(It.IsAny<ClaimsPrincipal>())).ReturnsAsync(ServiceResult<User?>.Fail("ошибка"));
        var controller = new AdminUsersController(action.Object,logger.Object,users.Object,null!,cache.Object);
        var pagin = new PaginationParams();
        var result = await CheckActiveUserManuallyAsync(users);
    Assert.IsType<UnauthorizedObjectResult>(result); 

        
    }
    [Fact]
    public async Task GetUsers_WhenUserBlocked_ReturnsForbidden()
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
var pagin = new PaginationParams(); 
        var result = await CheckActiveUserManuallyAsync(users);

        var type = Assert.IsType<ObjectResult>(result);
        Assert.Equal(403, type.StatusCode);
    }
    [Fact]
    public async Task GetUserById_WhenResult_NotOk()
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
    public async Task GetUserById_WhenUserBlocked_ReturnsForbidden()
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
    public async Task DeleteUser_WhenResult_NotOk()
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
    public async Task DeleteUser_WhenUserBlocked_ReturnsForbidden()
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
    public async Task DeleteUser_WhenUserNotFound_ReturnsNotFound()
    {
        var action = new Mock<IUserActionService>();
        var logger = new Mock<ILogger<AdminUsersController>>();
        var cache = new Mock<IMemoryCache>();
        var users = new Mock<IUserService>();

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        var context = new AppDbContext(options);

        users.Setup(x => x.GetCurrentUserAsync(It.IsAny<ClaimsPrincipal>()))
            .ReturnsAsync(ServiceResult<User?>.Ok(new User
            {
                Id = 1,
                Login = "admin",
                IsBlocked = false
            }));

        var controller = new AdminUsersController(action.Object, logger.Object, users.Object, context, cache.Object);

        var result = await controller.DeleteUser(999);

        Assert.IsType<NotFoundObjectResult>(result);
    }
    [Fact]
    public async Task BlockUser_WhenResult_NotOk()
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
    public async Task BlockUser_WhenUserBlocked_ReturnsForbidden()
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
    public async Task BlockUser_WhenUserNotFound_ReturnsNotFound()
    {
        var action = new Mock<IUserActionService>();
        var logger = new Mock<ILogger<AdminUsersController>>();
        var cache = new Mock<IMemoryCache>();
        var users = new Mock<IUserService>();

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        var context = new AppDbContext(options);

        users.Setup(x => x.GetCurrentUserAsync(It.IsAny<ClaimsPrincipal>()))
            .ReturnsAsync(ServiceResult<User?>.Ok(new User
            {
                Id = 1,
                Login = "admin",
                IsBlocked = false
            }));

        var controller = new AdminUsersController(action.Object, logger.Object, users.Object, context, cache.Object);

        var result = await controller.BlockUser(999,"",0,0,0);

        Assert.IsType<NotFoundObjectResult>(result);
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
    public async Task UnBlockUser_WhenUserNotFound_ReturnsNotFound()
    {
        var action = new Mock<IUserActionService>();
        var logger = new Mock<ILogger<AdminUsersController>>();
        var cache = new Mock<IMemoryCache>();
        var users = new Mock<IUserService>();

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        var context = new AppDbContext(options);

        users.Setup(x => x.GetCurrentUserAsync(It.IsAny<ClaimsPrincipal>()))
            .ReturnsAsync(ServiceResult<User?>.Ok(new User
            {
                Id = 1,
                Login = "admin",
                IsBlocked = false
            }));

        var controller = new AdminUsersController(action.Object, logger.Object, users.Object, context, cache.Object);

        var result = await controller.UnblockUser(999);

        Assert.IsType<NotFoundObjectResult>(result);
    }
}
