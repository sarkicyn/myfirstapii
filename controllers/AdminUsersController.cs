using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using MyApiBlya.Services;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;
[ApiController]
 [EnableRateLimiting("UserPolicy")] 
[Route("api/users")]
public class AdminUsersController : ControllerBase
{
    private readonly IUserService _users;
    private readonly ILogger<AdminUsersController> _logger;
    private readonly IUserActionService _action;
    private readonly AppDbContext _context;
    private readonly IMemoryCache _cache;

    public AdminUsersController(
        IUserActionService action,
        ILogger<AdminUsersController> logger,
        IUserService users,
        AppDbContext context,
        IMemoryCache cache)
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
 
    [ServiceFilter(typeof(ActiveUserFilter))]
    [HttpGet("{id:int:min(1):max(100)}")]
    public async Task<IActionResult> GetUserById(int id,CancellationToken token)
    {
        _logger.LogInformation("Запрос данных пользователя начат. Идентификатор запрошенного пользователя: {RequestedUserId}", id);

        var cacheKey = CacheKeys.UserById(id);
        var notFoundCacheKey = CacheKeys.UserNotFound(id);

        if (_cache.TryGetValue(notFoundCacheKey, out bool stub))
        {
            _logger.LogWarning("Запрошенный пользователь не найден. Идентификатор запрошенного пользователя: {RequestedUserId}", id);
            return NotFound(new { message = "Запрошенный пользователь не найден." });
        }

        if (!_cache.TryGetValue(cacheKey, out ServiceResult<User>? user))
        {
             user = await _users.GetUserByIdAsync(id,token);

            if (user == null || !user.Success)
            {
                _cache.Set(notFoundCacheKey, true, TimeSpan.FromSeconds(15));
                _logger.LogWarning("Запрошенный пользователь не найден. Идентификатор запрошенного пользователя: {RequestedUserId}", id);
                return NotFound(new { message = "Запрошенный пользователь не найден." });
            }

            _cache.Set(cacheKey, user, TimeSpan.FromMinutes(3));
        }

        var current = await _users.GetCurrentUserAsync(User,token);
        await _action.AddActionAsync(current.Data!, $"получение пользователя {user!.Data!.Login} по id",token);
        await _context.SaveChangesAsync();

        return Ok(user);
    }

    [Authorize(Roles = "Admin")]
    [ServiceFilter(typeof(ActiveUserFilter))]
    [HttpGet("all")]
    public async Task<IActionResult> GetUsers([FromQuery] PaginationParams pagination,CancellationToken token)
    {
        _logger.LogInformation("Запрос списка пользователей начат.");

        var currentUser = (await _users.GetCurrentUserAsync(User,token)).Data!;
        var users = await _users.GetAllUsersAsync(pagination,token);

        await _action.AddActionAsync(currentUser, "получить всех пользователей",token);

        _logger.LogInformation(
            "Запрос списка пользователей завершен. Идентификатор текущего пользователя: {CurrentUserId}, количество пользователей: {UsersCount}",
            currentUser.Id,
            users?.Data?.TotalCount ?? 0);

        return Ok(users!.Data);
    }

    [Authorize(Roles = "Admin")]
    [ServiceFilter(typeof(ActiveUserFilter))]
    [HttpDelete("deleteUser")]
    public async Task<IActionResult> DeleteUser(int Id,CancellationToken token)
    {
        var currentUser = (await _users.GetCurrentUserAsync(User,token)).Data!;
        var user = await _context.Users.FirstOrDefaultAsync(x => x.Id == Id,token);

        if (user != null)
        {
            await _action.AddActionAsync(currentUser, $"удаление пользователя {user.Login}",token);       
            _context.Users.Remove(user);
            await _context.SaveChangesAsync(token);
            RemoveUserCache(Id);

            _logger.LogInformation(
                "Пользователь удален. Идентификатор администратора: {AdminUserId}, идентификатор удаленного пользователя: {TargetUserId}",
                currentUser.Id,
                Id);

            return Ok(new { message = $"пользователь {user.Login} удален" });
        }

        return NotFound(new { message = "Пользователь не найден." });
    }

    [Authorize(Roles = "Admin")]
    [ServiceFilter(typeof(ActiveUserFilter))]
    [HttpPut("blockUser")]
    public async Task<IActionResult> BlockUser(int id,string Cause,int? Minutes,int? Hours,int? Days,CancellationToken token)
    {
var duration =
    TimeSpan.FromMinutes(Minutes ?? 0) +
    TimeSpan.FromHours(Hours ?? 0) +
    TimeSpan.FromDays(Days ?? 0);
       
        var user = await _context.Users.FirstOrDefaultAsync(x => x.Id == id,token);
if(user is not null && user.IsBlocked == true)
        {
            return Ok(new{message = "пользователь уже заблокирован"});
        }
        if (user != null)
        {
            user.IsBlocked = true;
            user.BlockedUntill = DateTime.UtcNow + duration;
            user.Cause = Cause;
            await _action.AddActionAsync(user, $"блокировка пользователя {user.Login}",token);
            await _context.SaveChangesAsync(token);
            RemoveUserCache(id);

            _logger.LogInformation(
                "Пользователь заблокирован. Идентификатор администратора: {AdminUserId}, идентификатор пользователя: {TargetUserId}",
                user.Id,
                id);
            return Ok(new BlockedUserResponseAdmin
            {
                Cause = Cause,
                DateBlock = DateTime.UtcNow + duration
            } );
        }

        return NotFound(new { message = "Пользователь не найден." });
    }

    [Authorize(Roles = "Admin")]
    [ServiceFilter(typeof(ActiveUserFilter))]
    [HttpPut("UnblockUser")]
    public async Task<IActionResult> UnblockUser(int id,CancellationToken token)
    {
        var currentUser = (await _users.GetCurrentUserAsync(User,token)).Data!;
        var user = await _context.Users.FirstOrDefaultAsync(x => x.Id == id,token);

        if (user != null)
        {
            user.IsBlocked = false;
            user.BlockedUntill = null;
            user.Cause = null; 
            await _action.AddActionAsync(currentUser, $"разблокировка пользователя {user.Login}",token);
            await _context.SaveChangesAsync(token);
            RemoveUserCache(id);

            _logger.LogInformation(
                "Пользователь разблокирован. Идентификатор администратора: {AdminUserId}, идентификатор пользователя: {TargetUserId}",
                currentUser.Id,
                id);

            return Ok(new { message = $"пользователь {user.Login} разблокирован" });
        }

        return NotFound(new { message = "Пользователь не найден." });
    }
}
