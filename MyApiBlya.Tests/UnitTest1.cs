namespace MyApiBlya.Tests;
using MyApiBlya.Tests;
using Moq;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using MyApiBlya.Services;
using System.Security.Claims;

public class UnitTest1
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
        var dto  = new LoginDTO
        {
            login = login,
            password = password
        }; 
var authServiceMock = new Mock<IAuthService>();
authServiceMock.Setup(x=>x.Login(It.IsAny<LoginDTO>())).ReturnsAsync(ServiceResult<LoginResponse>.Fail("неверно введены данные"));
var action = new Mock<IAddAction>();
var logger = new Mock<ILogger<AuthController>>();
var cache = new Mock<IMemoryCache>();
var users = new Mock<IUserService>();
users.Setup(x=>x.GetCurrentUserFromDatabaseAsync(It.IsAny<ClaimsPrincipal>())).ReturnsAsync(ServiceResult<User?>.Fail("пользователь не найден"));
var auth = new Mock<IAuthService>();
var controller = new AuthController(action.Object,logger.Object,users.Object,authServiceMock.Object,null!,cache.Object);
var result  = await controller.Login(dto);
Assert.IsType<BadRequestObjectResult>(result); 
authServiceMock.Verify(x=>x.Login(dto),Times.Never);
    }
}
