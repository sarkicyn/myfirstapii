using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using MyApiBlya.Services;

[ApiController]
[Route("api/users")]
public class AdminUsersController : ControllerBase
{
    private readonly IUserService _users;
    private readonly ILogger<AdminUsersController> _logger;
    private readonly IUserActionService _action;
    private readonly AppDbContext _context;
    private readonly IMemoryCache _cache;

    public AdminUsersController(IUserActionService action,ILogger<AdminUsersController> logger,
    IUserService users, AppDbContext context,IMemoryCache cache
    )
    {
        _context = context;
      _logger = logger;
      _users = users;
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


[Authorize(Roles = "Admin")]
    [HttpGet("{id:int:min(1):max(100)}")]
    public async Task<IActionResult> GetUserById(int id)
    {
        _logger.LogInformation("Р—Р°РїСЂРѕСЃ РґР°РЅРЅС‹С… РїРѕР»СЊР·РѕРІР°С‚РµР»СЏ РЅР°С‡Р°С‚. РРґРµРЅС‚РёС„РёРєР°С‚РѕСЂ Р·Р°РїСЂРѕС€РµРЅРЅРѕРіРѕ РїРѕР»СЊР·РѕРІР°С‚РµР»СЏ: {RequestedUserId}", id);

        var currentUser = await _users.GetCurrentUserAsync(User);
        if (!currentUser.Success || currentUser.Data is null)
        {
            _logger.LogWarning("РќРµР°РІС‚РѕСЂРёР·РѕРІР°РЅРЅС‹Р№ Р·Р°РїСЂРѕСЃ РґР°РЅРЅС‹С… РїРѕР»СЊР·РѕРІР°С‚РµР»СЏ. РРґРµРЅС‚РёС„РёРєР°С‚РѕСЂ Р·Р°РїСЂРѕС€РµРЅРЅРѕРіРѕ РїРѕР»СЊР·РѕРІР°С‚РµР»СЏ: {RequestedUserId}", id);
            return Unauthorized(new { message = "РўСЂРµР±СѓРµС‚СЃСЏ Р°РІС‚РѕСЂРёР·Р°С†РёСЏ." });
        }

        if (currentUser.Data!.IsBlocked)
        {
            _logger.LogWarning("Р”РµР№СЃС‚РІРёРµ Р·Р°РїСЂРµС‰РµРЅРѕ: С‚РµРєСѓС‰РёР№ РїРѕР»СЊР·РѕРІР°С‚РµР»СЊ Р·Р°Р±Р»РѕРєРёСЂРѕРІР°РЅ. РРґРµРЅС‚РёС„РёРєР°С‚РѕСЂ РїРѕР»СЊР·РѕРІР°С‚РµР»СЏ: {CurrentUserId}", currentUser.Data.Id);
            return StatusCode(StatusCodes.Status403Forbidden, new { message = "Р”РѕСЃС‚СѓРї Р·Р°РїСЂРµС‰РµРЅ." });
        }
        var cacheKey = CacheKeys.UserById(id);
        var notFoundCacheKey = CacheKeys.UserNotFound(id);

        if (_cache.TryGetValue(notFoundCacheKey, out bool _))
        {
            _logger.LogWarning("Р—Р°РїСЂРѕС€РµРЅРЅС‹Р№ РїРѕР»СЊР·РѕРІР°С‚РµР»СЊ РЅРµ РЅР°Р№РґРµРЅ. РРґРµРЅС‚РёС„РёРєР°С‚РѕСЂ Р·Р°РїСЂРѕС€РµРЅРЅРѕРіРѕ РїРѕР»СЊР·РѕРІР°С‚РµР»СЏ: {RequestedUserId}", id);
            return NotFound(new { message = "Р—Р°РїСЂРѕС€РµРЅРЅС‹Р№ РїРѕР»СЊР·РѕРІР°С‚РµР»СЊ РЅРµ РЅР°Р№РґРµРЅ." });
        }

        if (!_cache.TryGetValue(cacheKey, out User? user))
        {
            var ser = await _users.GetUserByIdAsync(id);

            if (ser == null || !ser.Success)
            {
                _cache.Set(notFoundCacheKey, true, TimeSpan.FromSeconds(15));
                _logger.LogWarning("Р—Р°РїСЂРѕС€РµРЅРЅС‹Р№ РїРѕР»СЊР·РѕРІР°С‚РµР»СЊ РЅРµ РЅР°Р№РґРµРЅ. РРґРµРЅС‚РёС„РёРєР°С‚РѕСЂ Р·Р°РїСЂРѕС€РµРЅРЅРѕРіРѕ РїРѕР»СЊР·РѕРІР°С‚РµР»СЏ: {RequestedUserId}", id);
                return NotFound(new { message = "Р—Р°РїСЂРѕС€РµРЅРЅС‹Р№ РїРѕР»СЊР·РѕРІР°С‚РµР»СЊ РЅРµ РЅР°Р№РґРµРЅ." });
            }

            user = ser.Data;
            _cache.Set(cacheKey,user,TimeSpan.FromMinutes(3));
        }

            await _action.AddActionAsync(currentUser.Data, $"РїРѕР»СѓС‡РµРЅРёРµ  РїРѕР»СЊР·РѕРІР°С‚РµР»СЏ {user!.Login} РїРѕ id");
            await _context.SaveChangesAsync();
            return Ok(user);
    }

[Authorize(Roles = "Admin")]
    [HttpGet("all")]
    public async Task<IActionResult> GetUsers()
    {
        _logger.LogInformation("Р—Р°РїСЂРѕСЃ СЃРїРёСЃРєР° РїРѕР»СЊР·РѕРІР°С‚РµР»РµР№ РЅР°С‡Р°С‚.");

        var currentUser = await _users.GetCurrentUserAsync(User);
        if (!currentUser.Success)
        {
            _logger.LogWarning("РќРµР°РІС‚РѕСЂРёР·РѕРІР°РЅРЅС‹Р№ Р·Р°РїСЂРѕСЃ СЃРїРёСЃРєР° РїРѕР»СЊР·РѕРІР°С‚РµР»РµР№.");
            return Unauthorized(new { message = "РўСЂРµР±СѓРµС‚СЃСЏ Р°РІС‚РѕСЂРёР·Р°С†РёСЏ." });
        }
        if (currentUser.Data!.IsBlocked)
        {
            _logger.LogWarning("Р”РµР№СЃС‚РІРёРµ Р·Р°РїСЂРµС‰РµРЅРѕ: С‚РµРєСѓС‰РёР№ РїРѕР»СЊР·РѕРІР°С‚РµР»СЊ Р·Р°Р±Р»РѕРєРёСЂРѕРІР°РЅ. РРґРµРЅС‚РёС„РёРєР°С‚РѕСЂ РїРѕР»СЊР·РѕРІР°С‚РµР»СЏ: {CurrentUserId}", currentUser.Data.Id);
            return StatusCode(StatusCodes.Status403Forbidden, new { message = "Р”РѕСЃС‚СѓРї Р·Р°РїСЂРµС‰РµРЅ." });
        }

        var users =  await _users.GetAllUsersAsync();

        await _action.AddActionAsync(currentUser.Data!, "РїРѕР»СѓС‡РёС‚СЊ РІСЃРµС… РїРѕР»СЊР·РѕРІР°С‚РµР»РµР№");
            await _context.SaveChangesAsync();


        _logger.LogInformation("Р—Р°РїСЂРѕСЃ СЃРїРёСЃРєР° РїРѕР»СЊР·РѕРІР°С‚РµР»РµР№ Р·Р°РІРµСЂС€РµРЅ. РРґРµРЅС‚РёС„РёРєР°С‚РѕСЂ С‚РµРєСѓС‰РµРіРѕ РїРѕР»СЊР·РѕРІР°С‚РµР»СЏ: {CurrentUserId}, РєРѕР»РёС‡РµСЃС‚РІРѕ РїРѕР»СЊР·РѕРІР°С‚РµР»РµР№: {UsersCount}", currentUser.Data!.Id, users?.Data?.Count ?? 0);
        return Ok(users);
    }

[Authorize(Roles = "Admin")]
[HttpDelete("deleteUser")]
public async Task<IActionResult> DeleteUser(int Id)
    {
        var currentUser = await _users.GetCurrentUserAsync(User);
        if (!currentUser.Success || currentUser.Data is null)
        {   
            return Unauthorized(new { message = "РўСЂРµР±СѓРµС‚СЃСЏ Р°РІС‚РѕСЂРёР·Р°С†РёСЏ." });
        }

        if (currentUser.Data.IsBlocked)
        {
            _logger.LogWarning("РЈРґР°Р»РµРЅРёРµ РїРѕР»СЊР·РѕРІР°С‚РµР»СЏ Р·Р°РїСЂРµС‰РµРЅРѕ: С‚РµРєСѓС‰РёР№ РїРѕР»СЊР·РѕРІР°С‚РµР»СЊ Р·Р°Р±Р»РѕРєРёСЂРѕРІР°РЅ. РРґРµРЅС‚РёС„РёРєР°С‚РѕСЂ РїРѕР»СЊР·РѕРІР°С‚РµР»СЏ: {CurrentUserId}, С†РµР»РµРІРѕР№ РїРѕР»СЊР·РѕРІР°С‚РµР»СЊ: {TargetUserId}", currentUser.Data.Id, Id);
            return StatusCode(StatusCodes.Status403Forbidden, new { message = "Р”РѕСЃС‚СѓРї Р·Р°РїСЂРµС‰РµРЅ." });
        }

        var user = await _context.Users.FirstOrDefaultAsync(x=>x.Id==Id);
        if (user != null)
        {
            await _action.AddActionAsync(currentUser.Data, $"СѓРґР°Р»РµРЅРёРµ РїРѕР»СЊР·РѕРІР°С‚РµР»СЏ {user.Login} ");
            _context.Users.Remove(user);
            await _context.SaveChangesAsync();
            RemoveUserCache(Id);
            _logger.LogInformation("РџРѕР»СЊР·РѕРІР°С‚РµР»СЊ СѓРґР°Р»РµРЅ. РРґРµРЅС‚РёС„РёРєР°С‚РѕСЂ Р°РґРјРёРЅРёСЃС‚СЂР°С‚РѕСЂР°: {AdminUserId}, РёРґРµРЅС‚РёС„РёРєР°С‚РѕСЂ СѓРґР°Р»РµРЅРЅРѕРіРѕ РїРѕР»СЊР·РѕРІР°С‚РµР»СЏ: {TargetUserId}", currentUser.Data.Id, Id);
            return Ok(new {message = $"РїРѕР»СЊР·РѕРІР°С‚РµР»СЊ {user.Login} СѓРґР°Р»РµРЅ"});
        }
            return NotFound(new {message = $"РџРѕР»СЊР·РѕРІР°С‚РµР»СЊ РЅРµ РЅР°Р№РґРµРЅ."});

    }

    [Authorize(Roles = "Admin")]
    [HttpPut("blockUser")]
    public async Task<IActionResult>BlockUser(int id)
    {
        var currentUser = await _users.GetCurrentUserAsync(User);
        if (!currentUser.Success || currentUser.Data is null)
        {
            return Unauthorized(new { message = "РўСЂРµР±СѓРµС‚СЃСЏ Р°РІС‚РѕСЂРёР·Р°С†РёСЏ." });
        }

        if (currentUser.Data.IsBlocked)
        {
            _logger.LogWarning("Р‘Р»РѕРєРёСЂРѕРІРєР° РїРѕР»СЊР·РѕРІР°С‚РµР»СЏ Р·Р°РїСЂРµС‰РµРЅР°: С‚РµРєСѓС‰РёР№ РїРѕР»СЊР·РѕРІР°С‚РµР»СЊ Р·Р°Р±Р»РѕРєРёСЂРѕРІР°РЅ. РРґРµРЅС‚РёС„РёРєР°С‚РѕСЂ РїРѕР»СЊР·РѕРІР°С‚РµР»СЏ: {CurrentUserId}, С†РµР»РµРІРѕР№ РїРѕР»СЊР·РѕРІР°С‚РµР»СЊ: {TargetUserId}", currentUser.Data.Id, id);
            return StatusCode(StatusCodes.Status403Forbidden, new { message = "Р”РѕСЃС‚СѓРї Р·Р°РїСЂРµС‰РµРЅ." });
        }

         var user = await _context.Users.FirstOrDefaultAsync(x=>x.Id==id);

        if (user != null)
        {
            user.IsBlocked = true;
            await _action.AddActionAsync(currentUser.Data, $"Р±Р»РѕРєРёСЂРѕРІРєР° РїРѕР»СЊР·РѕРІР°С‚РµР»СЏ {user.Login}");
            await _context.SaveChangesAsync();
            RemoveUserCache(id);
            _logger.LogInformation("РџРѕР»СЊР·РѕРІР°С‚РµР»СЊ Р·Р°Р±Р»РѕРєРёСЂРѕРІР°РЅ. РРґРµРЅС‚РёС„РёРєР°С‚РѕСЂ Р°РґРјРёРЅРёСЃС‚СЂР°С‚РѕСЂР°: {AdminUserId}, РёРґРµРЅС‚РёС„РёРєР°С‚РѕСЂ РїРѕР»СЊР·РѕРІР°С‚РµР»СЏ: {TargetUserId}", currentUser.Data.Id, id);

            return Ok(new{message = $"РїРѕР»СЊР·РѕРІР°С‚РµР»СЊ {user.Login} Р·Р°Р±Р»РѕРєРёСЂРѕРІР°РЅ"});
        }
        return NotFound(new{message ="РџРѕР»СЊР·РѕРІР°С‚РµР»СЊ РЅРµ РЅР°Р№РґРµРЅ."});
    }

    [Authorize(Roles = "Admin")]
    [HttpPut("UnblockUser")]
    public async Task<IActionResult>UnblockUser(int id)
    {
        var currentUser = await _users.GetCurrentUserAsync(User);
        if (!currentUser.Success || currentUser.Data is null)
        {
            return Unauthorized(new { message = "РўСЂРµР±СѓРµС‚СЃСЏ Р°РІС‚РѕСЂРёР·Р°С†РёСЏ." });
        }

        if (currentUser.Data.IsBlocked)
        {
            _logger.LogWarning("Р Р°Р·Р±Р»РѕРєРёСЂРѕРІРєР° РїРѕР»СЊР·РѕРІР°С‚РµР»СЏ Р·Р°РїСЂРµС‰РµРЅР°: С‚РµРєСѓС‰РёР№ РїРѕР»СЊР·РѕРІР°С‚РµР»СЊ Р·Р°Р±Р»РѕРєРёСЂРѕРІР°РЅ. РРґРµРЅС‚РёС„РёРєР°С‚РѕСЂ РїРѕР»СЊР·РѕРІР°С‚РµР»СЏ: {CurrentUserId}, С†РµР»РµРІРѕР№ РїРѕР»СЊР·РѕРІР°С‚РµР»СЊ: {TargetUserId}", currentUser.Data.Id, id);
            return StatusCode(StatusCodes.Status403Forbidden, new { message = "Р”РѕСЃС‚СѓРї Р·Р°РїСЂРµС‰РµРЅ." });
        }

         var user = await _context.Users.FirstOrDefaultAsync(x=>x.Id==id);
        if (user != null)
        {
            user.IsBlocked = false;
            await _action.AddActionAsync(currentUser.Data, $"СЂР°Р·Р±Р»РѕРєРёСЂРѕРІРєР° РїРѕР»СЊР·РѕРІР°С‚РµР»СЏ {user.Login}");
            await _context.SaveChangesAsync();
            RemoveUserCache(id);
            _logger.LogInformation("РџРѕР»СЊР·РѕРІР°С‚РµР»СЊ СЂР°Р·Р±Р»РѕРєРёСЂРѕРІР°РЅ. РРґРµРЅС‚РёС„РёРєР°С‚РѕСЂ Р°РґРјРёРЅРёСЃС‚СЂР°С‚РѕСЂР°: {AdminUserId}, РёРґРµРЅС‚РёС„РёРєР°С‚РѕСЂ РїРѕР»СЊР·РѕРІР°С‚РµР»СЏ: {TargetUserId}", currentUser.Data.Id, id);
            return Ok(new{message = $"РїРѕР»СЊР·РѕРІР°С‚РµР»СЊ {user.Login} СЂР°Р·Р±Р»РѕРєРёСЂРѕРІР°РЅ"});
        }
        return NotFound(new{message ="РџРѕР»СЊР·РѕРІР°С‚РµР»СЊ РЅРµ РЅР°Р№РґРµРЅ."});
    }
}


