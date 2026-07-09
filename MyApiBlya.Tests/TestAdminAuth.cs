namespace MyApiBlya.Tests;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using MyApiBlya.Services;

public class TestAdminAuth
{
    [Fact]
    public async Task AdminAuth_WhenResult_NotOk()
    {
        var token = CancellationToken.None;
        var dto = new LoginDto
        {
            Login = "wrong-admin",
            password = "wrong-password"
        };

        var auth = new Mock<IAuthService>();
        auth.Setup(x => x.AuthenticateAdminAsync(dto,token))
            .ReturnsAsync(ServiceResult<LoginResponse>.Fail("неверные данные"));

        var action = new Mock<IUserActionService>();
        var logger = new Mock<ILogger<AuthController>>();
        var cache = new Mock<IMemoryCache>();
        var users = new Mock<IUserService>();

        var controller = new AuthController(logger.Object, users.Object, auth.Object);

        var result = await controller.AuthenticateAdminAsync(dto,token);

        Assert.IsType<BadRequestObjectResult>(result);
        auth.Verify(x => x.AuthenticateAdminAsync(dto,token), Times.Once);
    }

    [Fact]
    public async Task AdminAuth_Service_WhenInvalidData_ReturnsFail()
    {
        var token = CancellationToken.None;
        var dto = new LoginDto
        {
            Login = "wrong-admin",
            password = "wrong-password"
        };

        var conf = new Mock<IConfiguration>();
        conf.Setup(x => x["ADMIN_LOGIN"]).Returns("admin");
        conf.Setup(x => x["ADMIN_PASSWORD"]).Returns("correct-password");

        var service = new AuthService(
            context: null!,
            cache: null!,
            logger: null!,
            action: null!,
            jwt: null!,
            fresh: null!,
            conf: conf.Object,
            HashPassword: null!,
            email: null!
        );

        var result = await service.AuthenticateAdminAsync(dto,token);

        Assert.False(result.Success);
        Assert.NotNull(result.Error);
        Assert.Null(result.Data);
    }
    
}

