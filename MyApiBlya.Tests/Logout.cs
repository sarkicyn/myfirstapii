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
using System.Security.Cryptography.X509Certificates;
using Microsoft.OpenApi;

public class logoutTest
{
    [Fact]
    public async Task Logout_WhenResult_NotOk()
    {
         var auth = new Mock<IAuthService>(); 
        var action = new Mock<IAddAction>();
var logger = new Mock<ILogger<AuthController>>();
var cache = new Mock<IMemoryCache>();
        var users = new Mock<IUserService>();
users.Setup(x=>x.GetCurrentUserFromDatabaseAsync(It.IsAny<ClaimsPrincipal>())).ReturnsAsync(ServiceResult<User?>.Fail("пользователь не найден"));

var controller = new AuthController(action.Object,logger.Object,users.Object,auth.Object,null!,cache.Object);
var result = await controller.Logout();
Assert.IsType<UnauthorizedObjectResult>(result);
    }
    [Fact]
    public async Task Logout_WhenUser_isBlocked()
    {
   
    
         var auth = new Mock<IAuthService>(); 
        var action = new Mock<IAddAction>();
var logger = new Mock<ILogger<AuthController>>();
var cache = new Mock<IMemoryCache>();
        var users = new Mock<IUserService>();
users.Setup(x=>x.GetCurrentUserFromDatabaseAsync(It.IsAny<ClaimsPrincipal>())).ReturnsAsync(ServiceResult<User?>.Ok(new User
{
    IsBlocked = true
}));

var controller = new AuthController(action.Object,logger.Object,users.Object,auth.Object,null!,cache.Object);
var result   = await controller.Logout(); 
var  objectResult= Assert.IsType<ObjectResult>(result);
        Assert.Equal(403, objectResult.StatusCode);
        
    }   
}