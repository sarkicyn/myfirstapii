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
        Refresh= "test-refresh-token"
    };
       var refresh = new Mock<IAuthService>();
       refresh.Setup(x=>x.Refresh(request)).ReturnsAsync(ServiceResult<string>.Fail("неверные данные"));
       var action = new Mock<IAddAction>();
var logger = new Mock<ILogger<AuthController>>();
var cache = new Mock<IMemoryCache>();
var users = new Mock<IUserService>();
var controller = new AuthController(action.Object,logger.Object,users.Object,refresh.Object,null!,cache.Object);
var result = await controller.Refresh(request);
Assert.IsType<BadRequestObjectResult>(result);
refresh.Verify(x=>x.Refresh(request),Times.Once);
 
    }
}