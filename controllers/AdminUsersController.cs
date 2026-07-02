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
    public async Task<IActionResult> GetUserById(int id)
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
             user = await _users.GetUserByIdAsync(id);

            if (user == null || !user.Success)
            {
                _cache.Set(notFoundCacheKey, true, TimeSpan.FromSeconds(15));
                _logger.LogWarning("Запрошенный пользователь не найден. Идентификатор запрошенного пользователя: {RequestedUserId}", id);
                return NotFound(new { message = "Запрошенный пользователь не найден." });
            }

            _cache.Set(cacheKey, user, TimeSpan.FromMinutes(3));
        }

        var current = await _users.GetCurrentUserAsync(User);
        await _action.AddActionAsync(current.Data!, $"получение пользователя {user!.Data!.Login} по id");
        await _context.SaveChangesAsync();

        return Ok(user);
    }

    [Authorize(Roles = "Admin")]
    [ServiceFilter(typeof(ActiveUserFilter))]
    [HttpGet("all")]
    public async Task<IActionResult> GetUsers([FromQuery] PaginationParams pagination)
    {
        _logger.LogInformation("Запрос списка пользователей начат.");

        var currentUser = (await _users.GetCurrentUserAsync(User)).Data!;
        var users = await _users.GetAllUsersAsync(pagination);

        await _action.AddActionAsync(currentUser, "получить всех пользователей");

        _logger.LogInformation(
            "Запрос списка пользователей завершен. Идентификатор текущего пользователя: {CurrentUserId}, количество пользователей: {UsersCount}",
            currentUser.Id,
            users?.Data?.TotalCount ?? 0);

        return Ok(users!.Data);
    }

    [Authorize(Roles = "Admin")]
    [ServiceFilter(typeof(ActiveUserFilter))]
    [HttpDelete("deleteUser")]
    public async Task<IActionResult> DeleteUser(int Id)
    {
        var currentUser = (await _users.GetCurrentUserAsync(User)).Data!;
        var user = await _context.Users.FirstOrDefaultAsync(x => x.Id == Id);

        if (user != null)
        {
            await _action.AddActionAsync(currentUser, $"удаление пользователя {user.Login}");
            _context.Users.Remove(user);
            await _context.SaveChangesAsync();
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
    public async Task<IActionResult> BlockUser(int id)
    {
        var currentUser = (await _users.GetCurrentUserAsync(User)).Data!;
        var user = await _context.Users.FirstOrDefaultAsync(x => x.Id == id);

        if (user != null)
        {
            user.IsBlocked = true;
            await _action.AddActionAsync(currentUser, $"блокировка пользователя {user.Login}");
            await _context.SaveChangesAsync();
            RemoveUserCache(id);

            _logger.LogInformation(
                "Пользователь заблокирован. Идентификатор администратора: {AdminUserId}, идентификатор пользователя: {TargetUserId}",
                currentUser.Id,
                id);

            return Ok(new { message = $"пользователь {user.Login} заблокирован" });
        }

        return NotFound(new { message = "Пользователь не найден." });
    }

    [Authorize(Roles = "Admin")]
    [ServiceFilter(typeof(ActiveUserFilter))]
    [HttpPut("UnblockUser")]
    public async Task<IActionResult> UnblockUser(int id)
    {
        var currentUser = (await _users.GetCurrentUserAsync(User)).Data!;
        var user = await _context.Users.FirstOrDefaultAsync(x => x.Id == id);

        if (user != null)
        {
            user.IsBlocked = false;
            await _action.AddActionAsync(currentUser, $"разблокировка пользователя {user.Login}");
            await _context.SaveChangesAsync();
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
