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
    private readonly IUserActionService _action;
    private readonly AppDbContext _context;
    private readonly IMemoryCache _cache;

    public AuthController(IUserActionService action,ILogger<AuthController> logger,
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
        public async Task<IActionResult> Login(LoginDto dTO)
        {
        var current = await _users.GetCurrentUserAsync(User);
        if (current.Success && current.Data is not null && current.Data.IsBlocked)
        {
            _logger.LogWarning("Р’С…РѕРґ Р·Р°РїСЂРµС‰РµРЅ: РїРѕР»СЊР·РѕРІР°С‚РµР»СЊ Р·Р°Р±Р»РѕРєРёСЂРѕРІР°РЅ. РРґРµРЅС‚РёС„РёРєР°С‚РѕСЂ РїРѕР»СЊР·РѕРІР°С‚РµР»СЏ: {CurrentUserId}", current.Data.Id);
            return StatusCode(StatusCodes.Status403Forbidden, new { message = "Р”РµР№СЃС‚РІРёРµ Р·Р°РїСЂРµС‰РµРЅРѕ: РІР°С€ Р°РєРєР°СѓРЅС‚ Р·Р°Р±Р»РѕРєРёСЂРѕРІР°РЅ." });
        }

        var result = await _auth.AuthenticateAsync(dTO);
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


       var result = await _auth.RefreshJwtAsync(request);
        if (!result.Success)
        {
            return ServiceResultMapper.ToActionResult(this,result);
        }
return Ok(result.Data);


    }

[Authorize]
    [HttpPost("logout")]
    public async Task<IActionResult> Logout()
    {

        _logger.LogInformation("Р—Р°РїСЂРѕСЃ РІС‹С…РѕРґР° РёР· Р°РєРєР°СѓРЅС‚Р° РЅР°С‡Р°С‚.");

        var currentUser = await _users.GetCurrentUserAsync(User);
        if (!currentUser.Success)
        {
            _logger.LogWarning("Р’С‹С…РѕРґ РЅРµ РІС‹РїРѕР»РЅРµРЅ: С‚РµРєСѓС‰РёР№ РїРѕР»СЊР·РѕРІР°С‚РµР»СЊ РЅРµ РЅР°Р№РґРµРЅ.");
            return Unauthorized(new { message = "РўСЂРµР±СѓРµС‚СЃСЏ Р°РІС‚РѕСЂРёР·Р°С†РёСЏ." });
        }
        if (currentUser.Data is not null && currentUser.Data.IsBlocked)
        {
            _logger.LogWarning("Р’С‹С…РѕРґ Р·Р°РїСЂРµС‰РµРЅ: РїРѕР»СЊР·РѕРІР°С‚РµР»СЊ Р·Р°Р±Р»РѕРєРёСЂРѕРІР°РЅ. РРґРµРЅС‚РёС„РёРєР°С‚РѕСЂ РїРѕР»СЊР·РѕРІР°С‚РµР»СЏ: {CurrentUserId}", currentUser.Data.Id);
            return StatusCode(StatusCodes.Status403Forbidden, new { message = "Р”РѕСЃС‚СѓРї Р·Р°РїСЂРµС‰РµРЅ." });
        }



            if(currentUser.Data!=null){
            var user = await _context.Users.FirstOrDefaultAsync(x=>x.Id==currentUser.Data!.Id);
            if (user != null)
            {
                user.RefreshTokenHash = null;

            }

        }




        await _action.AddActionAsync(currentUser.Data!, "РІС‹С…РѕРґ РёР· Р°РєРєР°СѓРЅС‚Р°");
        await _context.SaveChangesAsync();
        RemoveUserCache(currentUser.Data!.Id);


        _logger.LogInformation("Р—Р°РїСЂРѕСЃ РІС‹С…РѕРґР° РёР· Р°РєРєР°СѓРЅС‚Р° Р·Р°РІРµСЂС€РµРЅ. РРґРµРЅС‚РёС„РёРєР°С‚РѕСЂ С‚РµРєСѓС‰РµРіРѕ РїРѕР»СЊР·РѕРІР°С‚РµР»СЏ: {CurrentUserId}", currentUser.Data!.Id);
        return Ok(new { Message = "Logged out" });
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


