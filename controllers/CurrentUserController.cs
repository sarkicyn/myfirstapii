using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using MyApiBlya.Services;

[ApiController]
[Route("api/users")]
public class CurrentUserController : ControllerBase
{
    private readonly IUserService _users;
    private readonly ILogger<CurrentUserController> _logger;
    private readonly IUserActionService _action;
    private readonly AppDbContext _context;
    private readonly IMemoryCache _cache;

    public CurrentUserController(IUserActionService action,ILogger<CurrentUserController> logger,
    IUserService users, AppDbContext context,IMemoryCache cache
    )
    {
        _context = context;
      _logger = logger;
      _users = users;
      _action = action;
      _cache= cache;



    }


[Authorize]
    [HttpGet("me")]
    public async Task<IActionResult> GetCurrentUserProfileAsync()
    {

        var current = await _users.GetCurrentUserAsync(User);
        if (current.Success && current.Data is not null && current.Data.IsBlocked)
        {
            _logger.LogWarning("РџРѕР»СѓС‡РµРЅРёРµ РїСЂРѕС„РёР»СЏ Р·Р°РїСЂРµС‰РµРЅРѕ: РїРѕР»СЊР·РѕРІР°С‚РµР»СЊ Р·Р°Р±Р»РѕРєРёСЂРѕРІР°РЅ. РРґРµРЅС‚РёС„РёРєР°С‚РѕСЂ РїРѕР»СЊР·РѕРІР°С‚РµР»СЏ: {CurrentUserId}", current.Data.Id);
            return StatusCode(StatusCodes.Status403Forbidden, new { message = "Р”РµР№СЃС‚РІРёРµ Р·Р°РїСЂРµС‰РµРЅРѕ: РІР°С€ Р°РєРєР°СѓРЅС‚ Р·Р°Р±Р»РѕРєРёСЂРѕРІР°РЅ." });
        }
        _logger.LogInformation("Р—Р°РїСЂРѕСЃ СѓС‚РІРµСЂР¶РґРµРЅРёР№ С‚РµРєСѓС‰РµРіРѕ РїРѕР»СЊР·РѕРІР°С‚РµР»СЏ РЅР°С‡Р°С‚.");

       var result = await _users.GetCurrentUserProfileAsync(User);

        if (result.Success)
        {
await _action.AddActionAsync(current.Data!,"РїРѕР»СѓС‡РµРЅРёРµ РґР°РЅРЅС‹С… Рѕ РїРѕР»СЊР·РѕРІР°С‚РµР»Рµ");
await _context.SaveChangesAsync();

            return Ok(result.Data);
        }

        return Unauthorized(new { message = "РўСЂРµР±СѓРµС‚СЃСЏ Р°РІС‚РѕСЂРёР·Р°С†РёСЏ." });
    }

[Authorize]
    [HttpPut("rename")]
    public async Task<IActionResult> RenameUserAsync(string newLogin)
    {
        var current = await _users.GetCurrentUserAsync(User);
        if (current.Success && current.Data is not null && current.Data.IsBlocked)
        {
            _logger.LogWarning("РџРµСЂРµРёРјРµРЅРѕРІР°РЅРёРµ Р·Р°РїСЂРµС‰РµРЅРѕ: РїРѕР»СЊР·РѕРІР°С‚РµР»СЊ Р·Р°Р±Р»РѕРєРёСЂРѕРІР°РЅ. РРґРµРЅС‚РёС„РёРєР°С‚РѕСЂ РїРѕР»СЊР·РѕРІР°С‚РµР»СЏ: {CurrentUserId}", current.Data.Id);
            return StatusCode(StatusCodes.Status403Forbidden, new { message = "Р”РµР№СЃС‚РІРёРµ Р·Р°РїСЂРµС‰РµРЅРѕ: РІР°С€ Р°РєРєР°СѓРЅС‚ Р·Р°Р±Р»РѕРєРёСЂРѕРІР°РЅ." });
        }
        var result  = await _users.RenameUserAsync(current.Data!.Id, newLogin, User);
        if (result.Success)
        {
            return Ok(result);
        }

        return ServiceResultMapper.ToActionResult(this,result);


    }

    [Authorize]
    [HttpGet("history")]
   public async Task<IActionResult> GetHistory()
{
    var currentUser =
        await _users.GetCurrentUserAsync(User);
    if (!currentUser.Success || currentUser.Data is null)
    {
        return Unauthorized();
    }
        if (currentUser.Data is not null && currentUser.Data.IsBlocked)
        {
            _logger.LogWarning("РџРѕР»СѓС‡РµРЅРёРµ РёСЃС‚РѕСЂРёРё Р·Р°РїСЂРµС‰РµРЅРѕ: РїРѕР»СЊР·РѕРІР°С‚РµР»СЊ Р·Р°Р±Р»РѕРєРёСЂРѕРІР°РЅ. РРґРµРЅС‚РёС„РёРєР°С‚РѕСЂ РїРѕР»СЊР·РѕРІР°С‚РµР»СЏ: {CurrentUserId}", currentUser.Data.Id);
            return StatusCode(StatusCodes.Status403Forbidden, new { message = "Р”РѕСЃС‚СѓРї Р·Р°РїСЂРµС‰РµРЅ." });
        }
        var hasheKey = CacheKeys.UserHistory(currentUser.Data!.Id);
if (!_cache.TryGetValue(hasheKey, out List<UserHistoryDto>? newActions)){
        var moscowZone = TimeZoneInfo.FindSystemTimeZoneById("Russian Standard Time");
var  actions = await _context.UserActionHistories
    .Where(x => x.users_id == currentUser.Data!.Id)
    .Select(x => new  UserHistoryDto
    {
        action = x.UserAction != null ? x.UserAction.Action : null,
        time = x.CreatedAt

    })
    .ToListAsync();
    newActions=actions.Select(x=>new UserHistoryDto
    {
        action =x.action,
        time = TimeZoneInfo.ConvertTimeFromUtc(
            x.time,
            moscowZone)
    }).ToList();
    _cache.Set(hasheKey,actions,TimeSpan.FromSeconds(100));}
        if (newActions != null)
        {
            return Ok(newActions);
        }
        return Unauthorized();
}
}


