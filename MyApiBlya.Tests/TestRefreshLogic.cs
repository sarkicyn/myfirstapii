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


public class TestRefreshLogic()
{[Fact]
    public async Task Refresh_WhenService_ReturnBadRequest()
    {
    var request = new RefreshRequest
    {
        RefreshToken = "test-refresh-token"
    };
       var refreshAuthService = new Mock<IAuthService>();
       refreshAuthService.Setup(x=>x.RefreshAllTokens(request)).ReturnsAsync(ServiceResult<string>.Fail("неверные данные"));
       var action = new Mock<IUserActionService>();
var logger = new Mock<ILogger<AuthController>>();
var cache = new Mock<IMemoryCache>();
var users = new Mock<IUserService>();
var controller = new AuthController(action.Object,logger.Object,users.Object,refreshAuthService.Object,null!,cache.Object);
var result = await controller.Refresh(request);
Assert.IsType<BadRequestObjectResult>(result);
refreshAuthService.Verify(x=>x.RefreshAllTokens(request),Times.Once);
 
    }
    [Fact]
    public async Task Refresh_ServiceRefresh()
    {
        var service  = new AuthService(context:null!,cache:null!,logger:null!,action:null!,jwt:null!,fresh:null!,conf:null!,HashPassword:null!); 
            var request = new RefreshRequest
    {
        RefreshToken = ""
    };
    var result = await service.RefreshAllTokens(request); 
   Assert.IsType<ServiceResult<string>>(result);
        
    }
    [Fact]
    public async Task Refresh_WhenServiceRefresh_ReturnOk()
    {
        var request = new RefreshRequest
    {
        RefreshToken = "test-refresh-token"
    };

       var refreshAuthService = new Mock<IAuthService>();
       refreshAuthService.Setup(x=>x.RefreshAllTokens(request)).ReturnsAsync(ServiceResult<string>.Ok("jwt token"));
       var action = new Mock<IUserActionService>();
var logger = new Mock<ILogger<AuthController>>();
var cache = new Mock<IMemoryCache>();
var users = new Mock<IUserService>();
var controller = new AuthController(action.Object,logger.Object,users.Object,refreshAuthService.Object,null!,cache.Object);
var result = await controller.Refresh(request);
var type =Assert.IsType<OkObjectResult>(result);
var typeV = Assert.IsType<string>(type.Value);
Assert.Equal("jwt token",typeV);
    }
}


