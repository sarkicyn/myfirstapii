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

public class TestAuthLogic
{
    [Theory]
    [InlineData("", "12345")]
[InlineData("   ", "12345")]
[InlineData(null, "12345")]
[InlineData("test", "")]
[InlineData("test", "   ")]
[InlineData("test", null)]

    public async Task Login_WhenResult_NotOk(string? login, string? password)
    {
        var dto  = new LoginDto
        {
            Login = login,
            password = password
        }; 
var authServiceMock = new Mock<IAuthService>();
authServiceMock.Setup(x=>x.LoginAsync(It.IsAny<LoginDto>())).ReturnsAsync(ServiceResult<LoginResponse>.Fail("неверно введены данные"));
var action = new Mock<IUserActionService>();
var logger = new Mock<ILogger<AuthController>>();
var cache = new Mock<IMemoryCache>();
var users = new Mock<IUserService>();
users.Setup(x=>x.GetCurrentUserAsync(It.IsAny<ClaimsPrincipal>())).ReturnsAsync(ServiceResult<User?>.Fail("пользователь не найден"));
var auth = new Mock<IAuthService>();
var controller = new AuthController(logger.Object,users.Object,authServiceMock.Object);
var result  = await controller.Login(dto);
Assert.IsType<BadRequestObjectResult>(result); 
authServiceMock.Verify(x=>x.LoginAsync(dto),Times.Once);
    }

  [Theory]
    [InlineData("", "12345")]
[InlineData("   ", "12345")]
[InlineData(null, "12345")]
[InlineData("test", "")]
[InlineData("test", "   ")]
[InlineData("test", null)]
public async Task Login_WhenService_BadRequest(string? abdu, string? eblan)
    {

var dto = new LoginDto
{
    Login =abdu,
    password =eblan
};

        var service  = new AuthService(context:null!,cache:null!,logger:null!,action:null!,jwt:null!,fresh:null!,conf:null!,HashPassword:null!); 
        var result = await service.LoginAsync(dto);
        
Assert.False(result.Success);
Assert.NotNull(result.Error);
Assert.Null(result.Data);

    }
    [Fact]
    public async Task Login_WhenUserBlocked_ReturnForbid()
    {
        var users = new Mock<IUserService>();
        users.Setup(x=>x.GetCurrentUserAsync(It.IsAny<ClaimsPrincipal>())).ReturnsAsync( ServiceResult<User?>.Ok(new User
        { 
            Id = 1,
            IsBlocked = true
        }));
        var auth = new Mock<IAuthService>(); 
        var action = new Mock<IUserActionService>();
var logger = new Mock<ILogger<AuthController>>();
var cache = new Mock<IMemoryCache>();



var controller = new AuthController(logger.Object,users.Object,auth.Object);
var result = await controller.Login(new LoginDto
{
    Login = "artem",
    password = "krasivi"
    
});
 Assert.IsType<ObjectResult>(result);
auth.Verify(x=>x.LoginAsync(It.IsAny<LoginDto>()),Times.Never);
    }
    [Fact]
    public async Task Login_WhenResult_Ok()
    {
       var tokens  = new LoginResponse
       {
           Jwt = "jwt",
           RefreshToken = "refresh"
       };
       
       var dto = new LoginDto
       {
           Login = "jwt",
           password = "refresh"
       }; 
        var users = new Mock<IUserService>();
        var auth  = new Mock<IAuthService>();
        users.Setup(x=>x.GetCurrentUserAsync(It.IsAny<ClaimsPrincipal>())).ReturnsAsync(ServiceResult<User?>.Fail("ошибка"));
        auth.Setup(x=>x.LoginAsync(dto)).ReturnsAsync(ServiceResult<LoginResponse>.Ok(tokens));
          
        var action = new Mock<IUserActionService>();
var logger = new Mock<ILogger<AuthController>>();
var cache = new Mock<IMemoryCache>();



var controller = new AuthController(logger.Object,users.Object,auth.Object);
var result = await controller.Login(dto);
var type=Assert.IsType<OkObjectResult>(result);
var typeV=Assert.IsType<LoginResponse>(type.Value);
Assert.Equal("jwt",typeV.Jwt);
Assert.Equal("refresh",typeV.RefreshToken);



    }
}




