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
    private readonly IAddAction _action;
    private readonly AppDbContext _context;
    private readonly IMemoryCache _cache;

    public CurrentUserController(IAddAction action,ILogger<CurrentUserController> logger,
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
    public async Task<IActionResult> Me()
    {

        var current = await _users.GetCurrentUserFromDatabaseAsync(User);
        if (current.Success && current.Data is not null && current.Data.IsBlocked)
        {
            _logger.LogWarning("Получение профиля запрещено: пользователь заблокирован. Идентификатор пользователя: {CurrentUserId}", current.Data.Id);
            return StatusCode(StatusCodes.Status403Forbidden, new { message = "Действие запрещено: ваш аккаунт заблокирован." });
        }
        _logger.LogInformation("Запрос утверждений текущего пользователя начат.");

       var result = await _users.me(User);

        if (result.Success)
        {
await _action.AddActions(current.Data!,"получение данных о пользователе");
await _context.SaveChangesAsync();

            return Ok(result.Data);
        }

        return Unauthorized(new { message = "Требуется авторизация." });
    }

[Authorize]
    [HttpPut("rename")]
    public async Task<IActionResult> Rename( string rename)
    {
        var current = await _users.GetCurrentUserFromDatabaseAsync(User);
        if (current.Success && current.Data is not null && current.Data.IsBlocked)
        {
            _logger.LogWarning("Переименование запрещено: пользователь заблокирован. Идентификатор пользователя: {CurrentUserId}", current.Data.Id);
            return StatusCode(StatusCodes.Status403Forbidden, new { message = "Действие запрещено: ваш аккаунт заблокирован." });
        }
        var result  = await _users.Rename(current.Data!.Id,rename,User);
        if (result.Success)
        {
            return Ok(result);
        }

        return Servicehelper.ToActionRes(this,result);


    }

    [Authorize]
    [HttpGet("history")]
   public async Task<IActionResult> GetHistory()
{
    var currentUser =
        await _users.GetCurrentUserFromDatabaseAsync(User);
    if (!currentUser.Success || currentUser.Data is null)
    {
        return Unauthorized();
    }
        if (currentUser.Data is not null && currentUser.Data.IsBlocked)
        {
            _logger.LogWarning("Получение истории запрещено: пользователь заблокирован. Идентификатор пользователя: {CurrentUserId}", currentUser.Data.Id);
            return StatusCode(StatusCodes.Status403Forbidden, new { message = "Доступ запрещен." });
        }
        var hasheKey = CacheKeys.UserHistory(currentUser.Data!.Id);
if (!_cache.TryGetValue(hasheKey, out List<UserHistoryDto>? newActions)){
        var moscowZone = TimeZoneInfo.FindSystemTimeZoneById("Russian Standard Time");
var  actions = await _context.UsersHistory
    .Where(x => x.users_id == currentUser.Data!.Id)
    .Select(x => new  UserHistoryDto
    {
        action = x.history != null ? x.history.Action : null,
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
