using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Moq;
using MyApiBlya.Services;
using System.Security.Claims;
using Xunit.Sdk;

public class AdminUsersPostgresTests : IClassFixture<PostgresFixture>
{
    private readonly PostgresFixture _postgres;

    public AdminUsersPostgresTests(PostgresFixture postgres)
    {
        _postgres = postgres;
    }

    [Fact]
    public async Task DeleteUser_WhenResult_Ok()
    {
        var token = CancellationToken.None;
        await using var context = _postgres.CreateDbContext();

        var target = new User
        {
            Login = "user-to-delete",
            Password = "password",
            Role = "User",
            IsBlocked = false,
            CreatedAt = DateTime.UtcNow
        };

        var admin = new User
        {
            Login = "admin",
            Password = "password",
            Role = "Admin",
            IsBlocked = false,
            CreatedAt = DateTime.UtcNow
        };

        context.Users.Add(admin);
        context.Users.Add(target);
        await context.SaveChangesAsync();

        var action = new Mock<IUserActionService>();
        action.Setup(x => x.AddActionAsync(It.IsAny<User>(), It.IsAny<string>(),token))
            .Returns(Task.CompletedTask);

        var users = new Mock<IUserService>();
        users.Setup(x => x.GetCurrentUserAsync(It.IsAny<ClaimsPrincipal>(),token))
            .ReturnsAsync(ServiceResult<User?>.Ok(admin));

        var logger = new Mock<ILogger<AdminUsersController>>();
        var cache = new MemoryCache(new MemoryCacheOptions());

        var controller = new AdminUsersController(
            action.Object,
            logger.Object,
            users.Object,
            context,
            cache);

        var result = await controller.DeleteUser(target.Id,token);

        Assert.IsType<OkObjectResult>(result);

        var deletedUser = await context.Users.FindAsync(target.Id);
        Assert.Null(deletedUser);
    }

    [Fact]
    public async Task BlockUser_WhenResult_Ok()
    {
        var token = CancellationToken.None;
        await using var context = _postgres.CreateDbContext();

        var target = new User
        {
            Login = "user-to-block",
            Password = "password",
            Role = "User",
            IsBlocked = false,
            CreatedAt = DateTime.UtcNow
        };

        var admin = new User
        {
            Login = "admin-block",
            Password = "password",
            Role = "Admin",
            IsBlocked = false,
            CreatedAt = DateTime.UtcNow
        };

        context.Users.Add(admin);
        context.Users.Add(target);
        await context.SaveChangesAsync();

        var action = new Mock<IUserActionService>();
        action.Setup(x => x.AddActionAsync(It.IsAny<User>(), It.IsAny<string>(),token))
            .Returns(Task.CompletedTask);

        var users = new Mock<IUserService>();
        users.Setup(x => x.GetCurrentUserAsync(It.IsAny<ClaimsPrincipal>(),token))
            .ReturnsAsync(ServiceResult<User?>.Ok(admin));

        var logger = new Mock<ILogger<AdminUsersController>>();
        var cache = new MemoryCache(new MemoryCacheOptions());

        var controller = new AdminUsersController(
            action.Object,
            logger.Object,
            users.Object,
            context,
            cache);

        var result = await controller.BlockUser(target.Id,"",0,0,0,token);

        Assert.IsType<OkObjectResult>(result);

        var blockedUser = await context.Users.FindAsync(target.Id);
        Assert.NotNull(blockedUser);
        Assert.True(blockedUser.IsBlocked);
    }

    [Fact]
    public async Task UnblockUser_WhenResult_Ok()
    {
        var token = CancellationToken.None;
        await using var context = _postgres.CreateDbContext();

        var target = new User
        {
            Login = "user-to-unblock",
            Password = "password",
            Role = "User",
            IsBlocked = true,
            CreatedAt = DateTime.UtcNow
        };

        var admin = new User
        {
            Login = "admin-unblock",
            Password = "password",
            Role = "Admin",
            IsBlocked = false,
            CreatedAt = DateTime.UtcNow
        };

        context.Users.Add(admin);
        context.Users.Add(target);
        await context.SaveChangesAsync();

        var action = new Mock<IUserActionService>();
        action.Setup(x => x.AddActionAsync(It.IsAny<User>(), It.IsAny<string>(),token))
            .Returns(Task.CompletedTask);

        var users = new Mock<IUserService>();
        users.Setup(x => x.GetCurrentUserAsync(It.IsAny<ClaimsPrincipal>(),token))
            .ReturnsAsync(ServiceResult<User?>.Ok(admin));

        var logger = new Mock<ILogger<AdminUsersController>>();
        var cache = new MemoryCache(new MemoryCacheOptions());

        var controller = new AdminUsersController(
            action.Object,
            logger.Object,
            users.Object,
            context,
            cache);

        var result = await controller.UnblockUser(target.Id,token);

        Assert.IsType<OkObjectResult>(result);

        var unblockedUser = await context.Users.FindAsync(target.Id);
        Assert.NotNull(unblockedUser);
        Assert.False(unblockedUser.IsBlocked);
    }
    [Fact]
    public async Task ProfileDate_whenResult_Ok()
    {
        var token = CancellationToken.None;
        await using  var db = _postgres.CreateDbContext(); 

var logger = new Mock<ILogger<CurrentUserController>>();
        var cache = new MemoryCache(new MemoryCacheOptions());
        
        var currentUser = new ServiceResult<User?>
        {Data = new User{
            Login = "artem",
            IsBlocked = false}
            
        };
        db.Users.Add(currentUser.Data); 
        await db.SaveChangesAsync();
        var user=  new Mock<IUserService>();
        user.Setup(x=>x.GetCurrentUserAsync(It.IsAny<ClaimsPrincipal>(),token)).ReturnsAsync(currentUser);
        user.Setup(x=>x.GetCurrentUserProfileAsync(It.IsAny<ClaimsPrincipal>(),token)).ReturnsAsync(ServiceResult<CurrentUserProfileDto>.Ok(new CurrentUserProfileDto
        {Login= "artem",
        Role = "User"
            
        } ));
         var action = new Mock<IUserActionService>();
        action.Setup(x => x.AddActionAsync(It.IsAny<User>(), It.IsAny<string>(),token))
            .Returns(Task.CompletedTask);
            var controller= new CurrentUserController(action.Object,logger.Object,user.Object,db);
            var result = await controller.GetCurrentUserProfileAsync(token);
            var type = Assert.IsType<OkObjectResult>(result);
Assert.IsType<CurrentUserProfileDto>(type.Value);
            var userDb = db.Users.FindAsync(currentUser.Data.Id);
            
            Assert.NotNull(userDb);
    }
    [Fact]
    public async Task GetHistory_WhenResult_Ok()
    {
        var token = CancellationToken.None;
        await using var db = _postgres.CreateDbContext();

        var logger = new Mock<ILogger<CurrentUserController>>();
        var serviceLogger = new Mock<ILogger<UserService>>();
        var cache = new MemoryCache(new MemoryCacheOptions());
        var action = new Mock<IUserActionService>();
        var currentUser = new User
        {
            Login = $"artem-{Guid.NewGuid()}",
            IsBlocked = false,
            CreatedAt = DateTime.UtcNow
        };

        db.Users.Add(currentUser);
        var actions = new UserAction
        {
            Action = "read"
        };
        db.UserActions.Add(actions);
        await db.SaveChangesAsync();

        var usersHistories = new UserActionHistory
        {
            users_id = currentUser.Id,
            actions_id = actions.Id,
            CreatedAt = DateTime.UtcNow
        };
        db.UserActionHistories.Add(usersHistories);
        await db.SaveChangesAsync();

        var users = new Mock<IUserService>();
  users.Setup(x => x.GetUserHistoryAsync(It.IsAny<ClaimsPrincipal>(),token))
    .ReturnsAsync(ServiceResult<List<UserHistoryDto>>.Ok(new List<UserHistoryDto>
    {
        new UserHistoryDto
        {
            action = "read",
            time = DateTime.UtcNow
        }
    }));
        var controller = new CurrentUserController(action.Object, logger.Object, users.Object, db);
        
        var result = await controller.GetHistory(token);

        var ok = Assert.IsType<OkObjectResult>(result);
        var history = Assert.IsType<List<UserHistoryDto>>(ok.Value);
        var item = Assert.Single(history);
        Assert.Equal("read", item.action);
 


    }
}
