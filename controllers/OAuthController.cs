using AspNet.Security.OAuth.GitHub;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MyApiBlya.Services;

[ApiController]
[Route("api/users")]
public class OAuthController : ControllerBase
{
    private readonly IOAuthService _oauth;
    private readonly ILogger<OAuthController> _logger;

    public OAuthController(IOAuthService oauth, ILogger<OAuthController> logger)
    {
      _oauth = oauth;
      _logger = logger;
    }

    private static string BuildAuthCompleteRedirect(LoginResponse response)
    {
        var jwt = Uri.EscapeDataString(response.Jwt);
        var refreshToken = Uri.EscapeDataString(response.RefreshToken);

        return $"/api/users/auth-complete#jwt={jwt}&refresh={refreshToken}";
    }

[AllowAnonymous]
[ApiExplorerSettings(IgnoreApi = true)]
    [HttpGet("login/google")]
public IActionResult LoginGoogle()
{
    var properties = new AuthenticationProperties
    {
        RedirectUri = Url.Action(nameof(GoogleCallback))
    };

    return Challenge(properties, GoogleDefaults.AuthenticationScheme);
}

[AllowAnonymous]

[HttpGet("google-callback")]
[ApiExplorerSettings(IgnoreApi = true)]
public async Task<IActionResult> GoogleCallback()
{
    var result = await _oauth.HandleGoogleCallback();
        if (!result.Success)
        {
            if (result.Error == "account_blocked")
            {
                _logger.LogWarning("OAuth Google callback rejected: user account is blocked.");
                return StatusCode(StatusCodes.Status403Forbidden, new { message = "Р”РµР№СЃС‚РІРёРµ Р·Р°РїСЂРµС‰РµРЅРѕ: РІР°С€ Р°РєРєР°СѓРЅС‚ Р·Р°Р±Р»РѕРєРёСЂРѕРІР°РЅ." });
            }

            _logger.LogWarning("OAuth Google callback failed. Error: {Error}", result.Error);
            return BadRequest(new { message = result.Error ?? "РќРµ СѓРґР°Р»РѕСЃСЊ РІС‹РїРѕР»РЅРёС‚СЊ Р°СѓС‚РµРЅС‚РёС„РёРєР°С†РёСЋ." });
        }



    return Redirect(BuildAuthCompleteRedirect(result.Data!));
}

[AllowAnonymous]
[ApiExplorerSettings(IgnoreApi = true)]
[HttpGet("login/github")]
public IActionResult LoginGithub()
{

    var properties = new AuthenticationProperties
    {
        RedirectUri = Url.Action(nameof(GithubCallback))
    };

    return Challenge(
        properties,
        GitHubAuthenticationDefaults.AuthenticationScheme);
}

[AllowAnonymous]

[HttpGet("github-callback")]
[ApiExplorerSettings(IgnoreApi = true)]
public async Task<IActionResult> GithubCallback()
{

    var result = await _oauth.HandleGitHubCallback();
    if (!result.Success)
        {
            if (result.Error == "account_blocked")
            {
                _logger.LogWarning("OAuth GitHub callback rejected: user account is blocked.");
                return StatusCode(StatusCodes.Status403Forbidden, new { message = "Р”РµР№СЃС‚РІРёРµ Р·Р°РїСЂРµС‰РµРЅРѕ: РІР°С€ Р°РєРєР°СѓРЅС‚ Р·Р°Р±Р»РѕРєРёСЂРѕРІР°РЅ." });
            }

            _logger.LogWarning("OAuth GitHub callback failed. Error: {Error}", result.Error);
            return BadRequest(new { message = result.Error ?? "РќРµ СѓРґР°Р»РѕСЃСЊ РІС‹РїРѕР»РЅРёС‚СЊ Р°СѓС‚РµРЅС‚РёС„РёРєР°С†РёСЋ." });
        }
 return Redirect(BuildAuthCompleteRedirect(result.Data!));
}

[AllowAnonymous]
[HttpGet("auth-complete")]
[ApiExplorerSettings(IgnoreApi = true)]
public IActionResult AuthComplete()
{
    const string html = """
<!doctype html>
<html lang="ru">
<head>
    <meta charset="utf-8">
    <meta name="viewport" content="width=device-width, initial-scale=1">
    <title>Authentication complete</title>
    <style>
        body {
            margin: 0;
            min-height: 100vh;
            display: grid;
            place-items: center;
            font-family: Arial, sans-serif;
            background: #f4f6f8;
            color: #1f2937;
        }
        main {
            width: min(720px, calc(100vw - 32px));
            padding: 24px;
            background: #ffffff;
            border: 1px solid #d7dde5;
            border-radius: 8px;
        }
        h1 {
            margin: 0 0 18px;
            font-size: 24px;
        }
        label {
            display: block;
            margin: 16px 0 6px;
            font-weight: 700;
        }
        textarea {
            width: 100%;
            min-height: 86px;
            box-sizing: border-box;
            resize: vertical;
            font: 13px Consolas, monospace;
        }
        button {
            margin-top: 8px;
            padding: 8px 12px;
            cursor: pointer;
        }
        .error {
            color: #b42318;
        }
    </style>
</head>
<body>
    <main>
        <h1>Authentication complete</h1>
        <p id="status"></p>

        <label for="jwt">JWT</label>
        <textarea id="jwt" readonly></textarea>
        <button type="button" data-copy="jwt">Copy JWT</button>

        <label for="refresh">Refresh token</label>
        <textarea id="refresh" readonly></textarea>
        <button type="button" data-copy="refresh">Copy refresh</button>
    </main>

    <script>
        const params = new URLSearchParams(location.hash.slice(1));
        const jwt = params.get("jwt") || sessionStorage.getItem("jwt") || "";
        const refresh = params.get("refresh") || sessionStorage.getItem("refresh") || "";

        if (jwt) {
            sessionStorage.setItem("jwt", jwt);
        }

        if (refresh) {
            sessionStorage.setItem("refresh", refresh);
        }

        if (location.hash) {
            history.replaceState(null, document.title, location.pathname);
        }

        document.getElementById("jwt").value = jwt;
        document.getElementById("refresh").value = refresh;

        const status = document.getElementById("status");
        status.textContent = jwt && refresh
            ? "Tokens are available for this browser tab."
            : "No authentication data found. Start Google login again.";
        status.className = jwt && refresh ? "" : "error";

        document.querySelectorAll("[data-copy]").forEach((button) => {
            button.addEventListener("click", async () => {
                const id = button.getAttribute("data-copy");
                const value = document.getElementById(id).value;

                if (!value) {
                    return;
                }

                await navigator.clipboard.writeText(value);
                button.textContent = "Copied";
            });
        });
    </script>
</body>
</html>
""";

    return Content(html, "text/html; charset=utf-8");
}
}


