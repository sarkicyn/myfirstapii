using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using MyApiBlya.Services;

[ApiController]
[Route("api/users")]
public class AuthController : ControllerBase
{
    private readonly IUserService _users;
    private readonly IAuthService _auth;
    private readonly ILogger<AuthController> _logger;
    private readonly IAddAction _action;
    private readonly AppDbContext _context;
    private readonly IMemoryCache _cache;

    public AuthController(IAddAction action,ILogger<AuthController> logger,
    IUserService users, IAuthService auth, AppDbContext context,IMemoryCache cache
    )
    {
        _context = context;
      _logger = logger;
      _users = users;
      _auth = auth;
      _action = action;
      _cache = cache;



    }

    private void RemoveUserCache(int id)
    {
        _cache.Remove(CacheKeys.UserById(id));
        _cache.Remove(CacheKeys.UserNotFound(id));
        _cache.Remove(CacheKeys.CurrentUserById(id));
        _cache.Remove(CacheKeys.CurrentUserNotFound(id));
    }


    [AllowAnonymous]
    [HttpPost("login")]
        public async Task<IActionResult>Login(LoginDTO dTO)
        {
        var current = await _users.GetCurrentUserFromDatabaseAsync(User);
        if (current.Success && current.Data is not null && current.Data.IsBlocked)
        {
            _logger.LogWarning("Вход запрещен: пользователь заблокирован. Идентификатор пользователя: {CurrentUserId}", current.Data.Id);
            return StatusCode(StatusCodes.Status403Forbidden, new { message = "Действие запрещено: ваш аккаунт заблокирован." });
        }

        var result = await _auth.Login(dTO);
if (!result.Success)
        {
          return  Servicehelper.ToActionRes(this,result);
        }

        return Ok(result.Data);
    }

    [AllowAnonymous]
    [HttpPost("refresh")]
    public async Task<IActionResult> Refresh([FromBody] RefreshRequest request)
    {


       var result = await _auth.Refresh(request);
        if (!result.Success)
        {
            return Servicehelper.ToActionRes(this,result);
        }
return Ok(result.Data);


    }

[Authorize]
    [HttpPost("logout")]
    public async Task<IActionResult> Logout()
    {

        _logger.LogInformation("Запрос выхода из аккаунта начат.");

        var currentUser = await _users.GetCurrentUserFromDatabaseAsync(User);
        if (!currentUser.Success)
        {
            _logger.LogWarning("Выход не выполнен: текущий пользователь не найден.");
            return Unauthorized(new { message = "Требуется авторизация." });
        }
        if (currentUser.Data is not null && currentUser.Data.IsBlocked)
        {
            _logger.LogWarning("Выход запрещен: пользователь заблокирован. Идентификатор пользователя: {CurrentUserId}", currentUser.Data.Id);
            return StatusCode(StatusCodes.Status403Forbidden, new { message = "Доступ запрещен." });
        }



            if(currentUser.Data!=null){
            var user = await _context.users.FirstOrDefaultAsync(x=>x.Id==currentUser.Data!.Id);
            if (user != null)
            {
                user.RefreshTokenHash = null;

            }

        }




        await _action.AddActions(currentUser.Data!, "выход из аккаунта");
        await _context.SaveChangesAsync();
        RemoveUserCache(currentUser.Data!.Id);


        _logger.LogInformation("Запрос выхода из аккаунта завершен. Идентификатор текущего пользователя: {CurrentUserId}", currentUser.Data!.Id);
        return Ok(new { Message = "Logged out" });
    }

    [AllowAnonymous]
 [HttpPost("admin")]
 public async Task<IActionResult> AdminAuth(LoginDTO dto)
    {

        var admin = await _auth.AdminAuth(dto);
        if(admin.Success){
        return Ok(admin.Data);}
 return Servicehelper.ToActionRes(this,admin);
    }
}
