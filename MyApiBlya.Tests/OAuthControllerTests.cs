namespace MyApiBlya.Tests;

using AspNet.Security.OAuth.GitHub;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.Extensions.Logging;
using Moq;
using MyApiBlya.Services;

public class OAuthControllerTests
{
    [Fact]
    public async Task LoginGoogle_ReturnsGoogleChallenge_WithCallbackRedirect()
    {
        var oauth = new Mock<IOAuthService>();
        var logger = new Mock<ILogger<OAuthController>>();
        var url = new Mock<IUrlHelper>();

        url.Setup(x => x.Action(It.Is<UrlActionContext>(context =>
                context.Action == nameof(OAuthController.GoogleCallback))))
            .Returns("/api/users/google-callback");

        var controller = new OAuthController(oauth.Object, logger.Object)
        {
            Url = url.Object
        };

        var result =  controller.LoginGoogle();

        var challenge = Assert.IsType<ChallengeResult>(result);
        Assert.Contains(GoogleDefaults.AuthenticationScheme, challenge.AuthenticationSchemes);
        Assert.NotNull(challenge.Properties);
        Assert.Equal("/api/users/google-callback", challenge.Properties.RedirectUri);
    }
    [Fact]
    public async Task LogicGitHub_ReturnsGitHubchallenge_WithCallbackRedirect()
    {
        var oauth = new Mock<IOAuthService>();
        var logger = new Mock<ILogger<OAuthController>>();
        var url = new Mock< IUrlHelper>();
        url.Setup(x=>x.Action(It.Is<UrlActionContext>(context=>context.Action==nameof(OAuthController.GithubCallback)))).Returns("/api/users/github-callback");
        var controller = new OAuthController(oauth.Object, logger.Object)
        {
            Url = url.Object
        };
        var result = controller.LoginGithub();
        var type = Assert.IsType<ChallengeResult>(result);
        Assert.Contains(GitHubAuthenticationDefaults.AuthenticationScheme,type.AuthenticationSchemes);
        Assert.NotNull(type.Properties);
        Assert.Equal("/api/users/github-callback",type.Properties!.RedirectUri);  
    }
    [Fact]
    public async Task GoogleCallback_WhenReturns()
    {
        var auth  = new Mock<IOAuthService>();
        var loger = new Mock<ILogger<OAuthController>>();
        auth.Setup(x=>x.HandleGoogleCallback(It.IsAny<CancellationToken>())).ReturnsAsync(ServiceResult<LoginResponse>.Fail("ошибка"));
        var controller = new OAuthController(auth.Object,loger.Object);
    var result = await controller.GoogleCallback(); 
    Assert.IsType<BadRequestObjectResult>(result);
    
    }
    [Fact]
    public async Task GoogleCallback_WhenUserBlocked_ReturnsForbidden()
    {
        var auth = new Mock<IOAuthService>();
        var loger = new Mock<ILogger<OAuthController>>();

        auth.Setup(x => x.HandleGoogleCallback(It.IsAny<CancellationToken>()))
            .ReturnsAsync(ServiceResult<LoginResponse>.Fail(
                "Действие запрещено: ваш аккаунт заблокирован.",
                StatusCodes.Status403Forbidden));

        var controller = new OAuthController(auth.Object, loger.Object);

        var result = await controller.GoogleCallback();

        var type = Assert.IsType<ObjectResult>(result);
        Assert.Equal(403, type.StatusCode);
    }
    [Fact]
    public async Task GoogleCallback_WhenSuccess_ReturnsRedirectWithTokens()
    {
        var auth = new Mock<IOAuthService>();
        var loger = new Mock<ILogger<OAuthController>>();

        auth.Setup(x => x.HandleGoogleCallback(It.IsAny<CancellationToken>()))
            .ReturnsAsync(ServiceResult<LoginResponse>.Ok(new LoginResponse
            {
                Jwt = "jwt-token",
                RefreshToken = "refresh-token"
            }));

        var controller = new OAuthController(auth.Object, loger.Object);

        var result = await controller.GoogleCallback();

        var type = Assert.IsType<RedirectResult>(result);
        Assert.Equal("/api/users/auth-complete#jwt=jwt-token&refresh=refresh-token", type.Url);
    }
    [Fact]
    public async Task GithubCallback_WhenReturns()
    {
        var auth = new Mock<IOAuthService>();
        var loger = new Mock<ILogger<OAuthController>>();

        auth.Setup(x => x.HandleGitHubCallback(It.IsAny<CancellationToken>()))
            .ReturnsAsync(ServiceResult<LoginResponse>.Fail("ошибка"));

        var controller = new OAuthController(auth.Object, loger.Object);

        var result = await controller.GithubCallback();

        Assert.IsType<BadRequestObjectResult>(result);
    }
    [Fact]
    public async Task GithubCallback_WhenUserBlocked_ReturnsForbidden()
    {
        var auth = new Mock<IOAuthService>();
        var loger = new Mock<ILogger<OAuthController>>();

        auth.Setup(x => x.HandleGitHubCallback(It.IsAny<CancellationToken>()))
            .ReturnsAsync(ServiceResult<LoginResponse>.Fail(
                "Действие запрещено: ваш аккаунт заблокирован.",
                StatusCodes.Status403Forbidden));

        var controller = new OAuthController(auth.Object, loger.Object);

        var result = await controller.GithubCallback();

        var type = Assert.IsType<ObjectResult>(result);
        Assert.Equal(403, type.StatusCode);
    }
    [Fact]
    public async Task GithubCallback_WhenSuccess_ReturnsRedirectWithTokens()
    {
        var auth = new Mock<IOAuthService>();
        var loger = new Mock<ILogger<OAuthController>>();

        auth.Setup(x => x.HandleGitHubCallback(It.IsAny<CancellationToken>()))
            .ReturnsAsync(ServiceResult<LoginResponse>.Ok(new LoginResponse
            {
                Jwt = "jwt-token",
                RefreshToken = "refresh-token"
            }));

        var controller = new OAuthController(auth.Object, loger.Object);

        var result = await controller.GithubCallback();

        var type = Assert.IsType<RedirectResult>(result);
        Assert.Equal("/api/users/auth-complete#jwt=jwt-token&refresh=refresh-token", type.Url);
    }
}




