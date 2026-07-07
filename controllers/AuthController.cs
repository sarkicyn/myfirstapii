using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MyApiBlya.Services;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;
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
        public async Task<IActionResult> Login(LoginDto dTO)
        {
        var current = await _users.GetCurrentUserAsync(User);
        if (current.Success && current.Data is not null && current.Data.IsBlocked)
        {
            _logger.LogWarning("Вход запрещен: пользователь заблокирован. Идентификатор пользователя: {CurrentUserId}", current.Data.Id);
            return StatusCode(StatusCodes.Status403Forbidden, new { message = BlockedUserMessage.Create(current.Data) });
        }

        var result = await _auth.LoginAsync(dTO);
if (!result.Success)
        {
          return  ServiceResultMapper.ToActionResult(this,result);
        }

        return Ok(result.Data);
    }
    
    [AllowAnonymous]
    [HttpPost("register")]
        public async Task<IActionResult> Register(LoginDto dTO)
        {
        var current = await _users.GetCurrentUserAsync(User);
        if (current.Success && current.Data is not null && current.Data.IsBlocked)
        {
            _logger.LogWarning("Вход запрещен: пользователь заблокирован. Идентификатор пользователя: {CurrentUserId}", current.Data.Id);
            return StatusCode(StatusCodes.Status403Forbidden, new { message = BlockedUserMessage.Create(current.Data) });
        }

        var result = await _auth.RegisterAsync(dTO);
if (!result.Success)
        {
          return  ServiceResultMapper.ToActionResult(this,result);
        }

        return Ok(result.Data);
    }

    [AllowAnonymous]
    [HttpPost("refresh")]
    public async Task<IActionResult> Refresh([FromBody] RefreshRequest request)
    {


       var result = await _auth.RefreshAllTokens(request);
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
    public async Task<IActionResult> Logout()
    {
        var result = await _users.LogoutAsync(User);
        if (!result.Success)
        {
            return ServiceResultMapper.ToActionResult(this, result);
        }

        return Ok(new { Message = result.Data });
    }

    [AllowAnonymous]
 [HttpPost("admin")]
 public async Task<IActionResult> AuthenticateAdminAsync(LoginDto dto)
    {

        var admin = await _auth.AuthenticateAdminAsync(dto);
        if(admin.Success){
        return Ok(admin.Data);}
 return ServiceResultMapper.ToActionResult(this,admin);
    }
}
