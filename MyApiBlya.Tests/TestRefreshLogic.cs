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
using Npgsql.Replication;
using Microsoft.AspNetCore.Http;
using Castle.Core.Configuration;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi;

public class TestRefreshLogic()
{[Fact]
    public async Task Refresh_WhenService_ReturnBadRequest()
    {
    var token = CancellationToken.None;
    var request = new RefreshRequest
    {
        RefreshToken = "test-refresh-token"
    };
       var refreshAuthService = new Mock<IAuthService>();
       refreshAuthService.Setup(x=>x.RefreshAllTokens(request,token)).ReturnsAsync(ServiceResult<LoginResponse>.Fail("неверные данные"));
       var action = new Mock<IUserActionService>();
var logger = new Mock<ILogger<AuthController>>();
var cache = new Mock<IMemoryCache>();
var users = new Mock<IUserService>();
var controller = new AuthController(logger.Object,users.Object,refreshAuthService.Object);
var result = await controller.Refresh(request,token);
Assert.IsType<BadRequestObjectResult>(result);
refreshAuthService.Verify(x=>x.RefreshAllTokens(request,token),Times.Once);
 
    }
    [Theory]
[InlineData("")]
[InlineData(" ")]
[InlineData("   ")]
        public async Task Refresh_ServiceRefresh(string refresh)
    {
        var token = CancellationToken.None;
        var service  = new AuthService(context:null!,cache:null!,logger:null!,action:null!,jwt:null!,fresh:null!,conf:null!,HashPassword:null!,email:null!); 
   var request = new RefreshRequest
   {
       RefreshToken = refresh
   };
    var result = await service.RefreshAllTokens(request,token); 
   Assert.IsType<ServiceResult<LoginResponse>>(result);
        
    }
    [Fact]
    public async Task Refresh_WhenServiceRefresh_ReturnOk()
    {
        var token = CancellationToken.None;
        var request = new RefreshRequest
    {
        RefreshToken = "test-refresh-token"
    };

       var refreshAuthService = new Mock<IAuthService>();
       var res = "jwt token";
       refreshAuthService.Setup(x=>x.RefreshAllTokens(request,token)).ReturnsAsync(ServiceResult<LoginResponse>.Ok(new LoginResponse
       {
           Jwt = res
       }));
       var action = new Mock<IUserActionService>();
var logger = new Mock<ILogger<AuthController>>();
var cache = new Mock<IMemoryCache>();
var users = new Mock<IUserService>();
var controller = new AuthController(logger.Object,users.Object,refreshAuthService.Object);
var result = await controller.Refresh(request,token);
var type =Assert.IsType<OkObjectResult>(result);
var typeV = Assert.IsType<LoginResponse>(type.Value);
Assert.Equal("jwt token",typeV.Jwt);
    }
    [Fact]
public async Task RefreshAllTokens_WhenRefreshTokenNotFound_ReturnsUnauthorized()
{
    var token = CancellationToken.None;
    var options = new DbContextOptionsBuilder<AppDbContext>()
        .UseInMemoryDatabase(Guid.NewGuid().ToString())
        .Options;

    await using var context = new AppDbContext(options);

    var service = new AuthService(
        context,
        cache: null!,
        logger: null!,
        action: null!,
        jwt: null!,
        fresh: null!,
        conf: null!,
        HashPassword: null!,
        email: null!);

    var result = await service.RefreshAllTokens(new RefreshRequest
    {
        RefreshToken = "unknown-refresh-token"
    },token);

    Assert.False(result.Success);
    Assert.Equal(StatusCodes.Status401Unauthorized, result.StatusCode);
    Assert.Null(result.Data);
}
   
[Fact]
public async Task RefreshAllTokens_WhenRefreshTokenEmpty_ReturnsBadRequest()
{
    var token = CancellationToken.None;
    var service = new AuthService(
        context: null!,
        cache: null!,
        logger: null!,
        action: null!,
        jwt: null!,
        fresh: null!,
        conf: null!,
        HashPassword: null!,
        email: null!);

    var result = await service.RefreshAllTokens(new RefreshRequest
    {
        RefreshToken = ""
    },token);

    Assert.False(result.Success);
    Assert.Equal(StatusCodes.Status400BadRequest, result.StatusCode);
    Assert.Null(result.Data);
}
[Fact]
public async Task RefreshAllTokens_WhenTokenExpired_ReturnUnauthorized()
    {
        var token = CancellationToken.None;
        var jwt = new Mock<IJwtTokenService>();
       var refresh = new Mock<IRefreshTokenService>();
        
        var action = new Mock<IActionResult>();
            var options = new DbContextOptionsBuilder<AppDbContext>()
        .UseInMemoryDatabase(Guid.NewGuid().ToString())
        .Options;
        await using var context= new AppDbContext(options);
        
var auth = new AuthService(
    context,
    cache: null!,
    logger: null!,
    action:null!,
    jwt.Object,
  refresh.Object,
    conf: null!,
    HashPassword: null!,
    email: null!);
   context.Users.Add(new User
    {
        Login = "test",
        Password = "password",
        Role = "User",
        RefreshTokenHash = "refresh",
        RefreshTokenExpiresAt = DateTime.UtcNow.AddDays(-1),
        CreatedAt = DateTime.UtcNow
    });
    var request= new RefreshRequest
    {
        RefreshToken = "refresh"
    };
        await context.SaveChangesAsync();
   var result = await auth.RefreshAllTokens(request,token);
Assert.False(result.Success);
Assert.Equal(StatusCodes.Status401Unauthorized,result.StatusCode);
Assert.Null(result.Data);
jwt.Verify(x=>x.GenerateUserToken(It.IsAny<User>()),Times.Never);
refresh.Verify(x=>x.GenerateRefreshToken(),Times.Never);
    }

[Fact]
public async Task RefreshAllTokens_WhenRefreshTokenValid_ReturnsNewTokens()
{
    var token = CancellationToken.None;
    var jwt = new Mock<IJwtTokenService>();
    var refresh = new Mock<IRefreshTokenService>();
    var action = new Mock<IUserActionService>();

    var options = new DbContextOptionsBuilder<AppDbContext>()
        .UseInMemoryDatabase(Guid.NewGuid().ToString())
        .Options;

    await using var context = new AppDbContext(options);

    context.Users.Add(new User
    {
        Login = "test",
        Password = "password",
        Role = "User",
        RefreshTokenHash = "refresh",
        RefreshTokenExpiresAt = DateTime.UtcNow.AddDays(1),
        CreatedAt = DateTime.UtcNow
    });

    await context.SaveChangesAsync();

    jwt.Setup(x => x.GenerateUserToken(It.IsAny<User>()))
        .ReturnsAsync("new-jwt-token");

    refresh.Setup(x => x.GenerateRefreshToken())
        .Returns(("new-refresh-token", "new-refresh-token-hash"));

    refresh.Setup(x => x.SaveRefreshTokenAsync(It.IsAny<User>(), It.IsAny<string>(),token))
        .Returns(Task.CompletedTask);

    action.Setup(x => x.AddActionAsync(It.IsAny<User>(), It.IsAny<string>(),token))
        .Returns(Task.CompletedTask);

    var auth = new AuthService(
        context,
        cache: null!,
        logger: null!,
        action: action.Object,
        jwt: jwt.Object,
        fresh: refresh.Object,
        conf: null!,
        HashPassword: null!,
        email: null!);

    var result = await auth.RefreshAllTokens(new RefreshRequest
    {
        RefreshToken = "refresh"
    },token);

    Assert.NotNull(result);
    Assert.True(result.Success);
    Assert.NotNull(result.Data);
    Assert.Equal("new-jwt-token", result.Data.Jwt);
    Assert.Equal("new-refresh-token", result.Data.RefreshToken);

    jwt.Verify(x => x.GenerateUserToken(It.IsAny<User>()), Times.Once);
    refresh.Verify(x => x.GenerateRefreshToken(), Times.Once);
}

[Fact]
public async Task RefreshAllTokens_WhenAdminRefreshTokenValid_ReturnsNewTokens()
{
    var token = CancellationToken.None;
    var jwt = new Mock<IJwtTokenService>();
    var refresh = new Mock<IRefreshTokenService>();
    var action = new Mock<IUserActionService>();

    var options = new DbContextOptionsBuilder<AppDbContext>()
        .UseInMemoryDatabase(Guid.NewGuid().ToString())
        .Options;

    await using var context = new AppDbContext(options);

    context.Users.Add(new User
    {
        Login = "admin",
        Password = "password",
        Role = "Admin",
        RefreshTokenHash = "admin-refresh",
        RefreshTokenExpiresAt = DateTime.UtcNow.AddDays(1),
        CreatedAt = DateTime.UtcNow
    });

    await context.SaveChangesAsync();

    jwt.Setup(x => x.GenerateAdminToken(It.IsAny<User>()))
        .ReturnsAsync("new-admin-jwt-token");

    refresh.Setup(x => x.GenerateRefreshToken())
        .Returns(("new-admin-refresh-token", "new-admin-refresh-token-hash"));

    refresh.Setup(x => x.SaveRefreshTokenAsync(It.IsAny<User>(), It.IsAny<string>(),token))
        .Returns(Task.CompletedTask);

    action.Setup(x => x.AddActionAsync(It.IsAny<User>(), It.IsAny<string>(),token))
        .Returns(Task.CompletedTask);

    var auth = new AuthService(
        context,
        cache: null!,
        logger: null!,
        action: action.Object,
        jwt: jwt.Object,
        fresh: refresh.Object,
        conf: null!,
        HashPassword: null!,
        email: null!);

    var result = await auth.RefreshAllTokens(new RefreshRequest
    {
        RefreshToken = "admin-refresh"
    },token);

    Assert.NotNull(result);
    Assert.True(result.Success);
    Assert.NotNull(result.Data);
    Assert.Equal("new-admin-jwt-token", result.Data.Jwt);
    Assert.Equal("new-admin-refresh-token", result.Data.RefreshToken);

    jwt.Verify(x => x.GenerateAdminToken(It.IsAny<User>()), Times.Once);
    refresh.Verify(x => x.GenerateRefreshToken(), Times.Once);
}
    
}


