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
        _logger.LogInformation("Запрос данных пользователя начат. Идентификатор запрошенного пользователя: {RequestedUserId}", id);

        var currentUser = await _users.GetCurrentUserAsync(User);
        if (!currentUser.Success || currentUser.Data is null)
        {
            _logger.LogWarning("Неавторизованный запрос данных пользователя. Идентификатор запрошенного пользователя: {RequestedUserId}", id);
            return Unauthorized(new { message = "Требуется авторизация." });
        }

        if (currentUser.Data!.IsBlocked)
        {
            _logger.LogWarning("Действие запрещено: текущий пользователь заблокирован. Идентификатор пользователя: {CurrentUserId}", currentUser.Data.Id);
            return StatusCode(StatusCodes.Status403Forbidden, new { message = "Доступ запрещен." });
        }
        var cacheKey = CacheKeys.UserById(id);
        var notFoundCacheKey = CacheKeys.UserNotFound(id);

        if (_cache.TryGetValue(notFoundCacheKey, out bool _))
        {
            _logger.LogWarning("Запрошенный пользователь не найден. Идентификатор запрошенного пользователя: {RequestedUserId}", id);
            return NotFound(new { message = "Запрошенный пользователь не найден." });
        }

        if (!_cache.TryGetValue(cacheKey, out User? user))
        {
            var ser = await _users.GetUserByIdAsync(id);

            if (ser == null || !ser.Success)
            {
                _cache.Set(notFoundCacheKey, true, TimeSpan.FromSeconds(15));
                _logger.LogWarning("Запрошенный пользователь не найден. Идентификатор запрошенного пользователя: {RequestedUserId}", id);
                return NotFound(new { message = "Запрошенный пользователь не найден." });
            }

            user = ser.Data;
            _cache.Set(cacheKey,user,TimeSpan.FromMinutes(3));
        }

            await _action.AddActionAsync(currentUser.Data, $"получение  пользователя {user!.Login} по id");
            await _context.SaveChangesAsync();
            return Ok(user);
    }

[Authorize(Roles = "Admin")]
    [HttpGet("all")]
    public async Task<IActionResult> GetUsers()
    {
        _logger.LogInformation("Запрос списка пользователей начат.");

        var currentUser = await _users.GetCurrentUserAsync(User);
        if (!currentUser.Success)
        {
            _logger.LogWarning("Неавторизованный запрос списка пользователей.");
            return Unauthorized(new { message = "Требуется авторизация." });
        }
        if (currentUser.Data!.IsBlocked)
        {
            _logger.LogWarning("Действие запрещено: текущий пользователь заблокирован. Идентификатор пользователя: {CurrentUserId}", currentUser.Data.Id);
            return StatusCode(StatusCodes.Status403Forbidden, new { message = "Доступ запрещен." });
        }

        var users =  await _users.GetAllUsersAsync();

        await _action.AddActionAsync(currentUser.Data!, "получить всех пользователей");
            await _context.SaveChangesAsync();


        _logger.LogInformation("Запрос списка пользователей завершен. Идентификатор текущего пользователя: {CurrentUserId}, количество пользователей: {UsersCount}", currentUser.Data!.Id, users?.Data?.Count ?? 0);
        return Ok(users!.Data);
    }

[Authorize(Roles = "Admin")]
[HttpDelete("deleteUser")]
public async Task<IActionResult> DeleteUser(int Id)
    {
        var currentUser = await _users.GetCurrentUserAsync(User);
        if (!currentUser.Success || currentUser.Data is null)
        {   
            return Unauthorized(new { message = "Требуется авторизация." });
        }

        if (currentUser.Data.IsBlocked)
        {
            _logger.LogWarning("Удаление пользователя запрещено: текущий пользователь заблокирован. Идентификатор пользователя: {CurrentUserId}, целевой пользователь: {TargetUserId}", currentUser.Data.Id, Id);
            return StatusCode(StatusCodes.Status403Forbidden, new { message = "Доступ запрещен." });
        }

        var user = await _context.Users.FirstOrDefaultAsync(x=>x.Id==Id);
        if (user != null)
        {
            await _action.AddActionAsync(currentUser.Data, $"удаление пользователя {user.Login} ");
            _context.Users.Remove(user);
            await _context.SaveChangesAsync();
            RemoveUserCache(Id);
            _logger.LogInformation("Пользователь удален. Идентификатор администратора: {AdminUserId}, идентификатор удаленного пользователя: {TargetUserId}", currentUser.Data.Id, Id);
            return Ok(new {message = $"пользователь {user.Login} удален"});
        }
            return NotFound(new {message = $"Пользователь не найден."});

    }

    [Authorize(Roles = "Admin")]
    [HttpPut("blockUser")]
    public async Task<IActionResult>BlockUser(int id)
    {
        var currentUser = await _users.GetCurrentUserAsync(User);
        if (!currentUser.Success || currentUser.Data is null)
        {
            return Unauthorized(new { message = "Требуется авторизация." });
        }

        if (currentUser.Data.IsBlocked)
        {
            _logger.LogWarning("Блокировка пользователя запрещена: текущий пользователь заблокирован. Идентификатор пользователя: {CurrentUserId}, целевой пользователь: {TargetUserId}", currentUser.Data.Id, id);
            return StatusCode(StatusCodes.Status403Forbidden, new { message = "Доступ запрещен." });
        }

         var user = await _context.Users.FirstOrDefaultAsync(x=>x.Id==id);

        if (user != null)
        {
            user.IsBlocked = true;
            await _action.AddActionAsync(currentUser.Data, $"блокировка пользователя {user.Login}");
            await _context.SaveChangesAsync();
            RemoveUserCache(id);
            _logger.LogInformation("Пользователь заблокирован. Идентификатор администратора: {AdminUserId}, идентификатор пользователя: {TargetUserId}", currentUser.Data.Id, id);

            return Ok(new{message = $"пользователь {user.Login} заблокирован"});
        }
        return NotFound(new{message ="Пользователь не найден."});
    }

    [Authorize(Roles = "Admin")]
    [HttpPut("UnblockUser")]
    public async Task<IActionResult>UnblockUser(int id)
    {
        var currentUser = await _users.GetCurrentUserAsync(User);
        if (!currentUser.Success || currentUser.Data is null)
        {
            return Unauthorized(new { message = "Требуется авторизация." });
        }

        if (currentUser.Data.IsBlocked)
        {
            _logger.LogWarning("Разблокировка пользователя запрещена: текущий пользователь заблокирован. Идентификатор пользователя: {CurrentUserId}, целевой пользователь: {TargetUserId}", currentUser.Data.Id, id);
            return StatusCode(StatusCodes.Status403Forbidden, new { message = "Доступ запрещен." });
        }

         var user = await _context.Users.FirstOrDefaultAsync(x=>x.Id==id);
        if (user != null)
        {
            user.IsBlocked = false;
            await _action.AddActionAsync(currentUser.Data, $"разблокировка пользователя {user.Login}");
            await _context.SaveChangesAsync();
            RemoveUserCache(id);
            _logger.LogInformation("Пользователь разблокирован. Идентификатор администратора: {AdminUserId}, идентификатор пользователя: {TargetUserId}", currentUser.Data.Id, id);
            return Ok(new{message = $"пользователь {user.Login} разблокирован"});
        }
        return NotFound(new{message ="Пользователь не найден."});
    }
}


