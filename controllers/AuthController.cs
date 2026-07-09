using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MyApiBlya.Services;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;
using System.ComponentModel;
[ApiController]
 [EnableRateLimiting("IpPolicy")] 

[Route("api/users")]
public class AuthController : ControllerBase
{
    private readonly IUserService _users;
    private readonly IAuthService _auth;
    private readonly ILogger<AuthController> _logger;

    public AuthController(ILogger<AuthController> logger,
    IUserService users, IAuthService auth
    )
    {
      _logger = logger;
      _users = users;
      _auth = auth;
    }


    [AllowAnonymous]
    [HttpPost("login")]
        public async Task<IActionResult> Login(LoginDto dTO,CancellationToken token)
        {
        var current = await _users.GetCurrentUserAsync(User,token);
        if (current.Success && current.Data is not null && current.Data.IsBlocked)
        {
            _logger.LogWarning("Вход запрещен: пользователь заблокирован. Идентификатор пользователя: {CurrentUserId}", current.Data.Id);
            return StatusCode(StatusCodes.Status403Forbidden, new { message = BlockedUserMessage.Create(current.Data) });
        }

        var result = await _auth.LoginAsync(dTO,token);
if (!result.Success)
        {
          return  ServiceResultMapper.ToActionResult(this,result);
        }

        return Ok(result.Data);
    }
    
    [AllowAnonymous]
    [HttpPost("register")]
        public async Task<IActionResult> Register(LoginDto dTO,CancellationToken token)
        {
        var current = await _users.GetCurrentUserAsync(User,token);
        if (current.Success && current.Data is not null && current.Data.IsBlocked)
        {
            _logger.LogWarning("Вход запрещен: пользователь заблокирован. Идентификатор пользователя: {CurrentUserId}", current.Data.Id);
            return StatusCode(StatusCodes.Status403Forbidden, new { message = BlockedUserMessage.Create(current.Data) });
        }

        var result = await _auth.RegisterAsync(dTO,token);
if (!result.Success)
        {
          return  ServiceResultMapper.ToActionResult(this,result);
        }

        return Ok(result.Data);
    }

    [AllowAnonymous]
    [HttpPost("refresh")]
    public async Task<IActionResult> Refresh([FromBody] RefreshRequest request,CancellationToken token)
    {


       var result = await _auth.RefreshAllTokens(request,token);
        if (!result.Success)
        {
            return ServiceResultMapper.ToActionResult(this,result);
        }
return Ok(result.Data);


    }

[Authorize]
 [EnableRateLimiting("UserPolicy")] 

    [ServiceFilter(typeof(ActiveUserFilter))]
    [HttpPost("logout")]
    public async Task<IActionResult> Logout(CancellationToken token)
    {
        var result = await _users.LogoutAsync(User,token);
        if (!result.Success)
        {
            return ServiceResultMapper.ToActionResult(this, result);
        }

        return Ok(new { Message = result.Data });
    }

    [AllowAnonymous]
 [HttpPost("admin")]
 public async Task<IActionResult> AuthenticateAdminAsync(LoginDto dto,CancellationToken token)
    {

        var admin = await _auth.AuthenticateAdminAsync(dto,token);
        if(admin.Success){
        return Ok(admin.Data);}
 return ServiceResultMapper.ToActionResult(this,admin);
    }
}
