using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Moq;
using MyApiBlya.Services;
using System.Security.Claims;

public class AdminUsersPostgresTests : IClassFixture<PostgresFixture>
{
    private readonly PostgresFixture _postgres;

    public AdminUsersPostgresTests(PostgresFixture postgres)
    {
        _postgres = postgres;
    }

    [Fact]
    [Trait("Category", "Postgres")]
    public async Task DeleteUser_WhenResult_Ok()
    {
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
        action.Setup(x => x.AddActionAsync(It.IsAny<User>(), It.IsAny<string>()))
            .Returns(Task.CompletedTask);

        var users = new Mock<IUserService>();
        users.Setup(x => x.GetCurrentUserAsync(It.IsAny<ClaimsPrincipal>()))
            .ReturnsAsync(ServiceResult<User?>.Ok(admin));

        var logger = new Mock<ILogger<AdminUsersController>>();
        var cache = new MemoryCache(new MemoryCacheOptions());

        var controller = new AdminUsersController(
            action.Object,
            logger.Object,
            users.Object,
            context,
            cache);

        var result = await controller.DeleteUser(target.Id);

        Assert.IsType<OkObjectResult>(result);

        var deletedUser = await context.Users.FindAsync(target.Id);
        Assert.Null(deletedUser);
    }
}
