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
using System.Security.Cryptography.X509Certificates;
using Microsoft.OpenApi;

public class logoutTest
{
    [Fact]
    public async Task Logout_WhenResult_NotOk()
    {
        var token = CancellationToken.None;
         var auth = new Mock<IAuthService>(); 
        var action = new Mock<IUserActionService>();
var logger = new Mock<ILogger<AuthController>>();
var cache = new Mock<IMemoryCache>();
        var users = new Mock<IUserService>();
users.Setup(x=>x.LogoutAsync(It.IsAny<ClaimsPrincipal>(),token)).ReturnsAsync(ServiceResult<string>.Fail("Требуется авторизация.", StatusCodes.Status401Unauthorized));

var controller = new AuthController(logger.Object,users.Object,auth.Object);
var result = await controller.Logout(token);
Assert.IsType<UnauthorizedObjectResult>(result);
    }
    [Fact]
    public async Task Logout_WhenUser_isBlocked()
    {
        var token = CancellationToken.None;
   
    
         var auth = new Mock<IAuthService>(); 
        var action = new Mock<IUserActionService>();
var logger = new Mock<ILogger<AuthController>>();
var cache = new Mock<IMemoryCache>();
        var users = new Mock<IUserService>();
users.Setup(x=>x.LogoutAsync(It.IsAny<ClaimsPrincipal>(),token)).ReturnsAsync(ServiceResult<string>.Fail("Доступ запрещен.", StatusCodes.Status403Forbidden));

var controller = new AuthController(logger.Object,users.Object,auth.Object);
var result   = await controller.Logout(token); 
var  objectResult= Assert.IsType<ObjectResult>(result);
        Assert.Equal(403, objectResult.StatusCode);
        
    }   
}
