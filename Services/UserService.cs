using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using MyApiBlya.Services;
using System.Security.Claims;
using System.Security.Principal;

public class UserService : IUserService
{
     private readonly AppDbContext _context;
 private readonly IMemoryCache _cache;
    private readonly ILogger<UserService> _logger;
    private readonly IUserActionService _action;

    public UserService(AppDbContext context,IMemoryCache cache,ILogger<UserService> logger,IUserActionService action)
    {
        _context  = context;
        _cache = cache;
        _logger = logger; 
        _action = action;
    }
        
    private void RemoveUserCache(int id)
    {
        _cache.Remove(CacheKeys.UserById(id));
        _cache.Remove(CacheKeys.UserNotFound(id));
        _cache.Remove(CacheKeys.CurrentUserById(id));
        _cache.Remove(CacheKeys.CurrentUserNotFound(id));
    }
     public async Task<ServiceResult<User>> GetUserByIdAsync(int id,CancellationToken token)
    { 
        
        var cacheKey = CacheKeys.UserById(id);
        var notFoundCacheKey = CacheKeys.UserNotFound(id);

        if (_cache.TryGetValue(notFoundCacheKey, out bool _))
        {
            return ServiceResult<User>.Fail("Пользователь не найден.", StatusCodes.Status404NotFound);
        }

        if (!_cache.TryGetValue(cacheKey, out User? user))
        {
            _logger.LogInformation("Пользователь не найден в кэше. Идентификатор запрошенного пользователя: {RequestedUserId}", id);
            user = await _context.Users
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == id);
            if (user == null)
            {
                _cache.Set(notFoundCacheKey, true, TimeSpan.FromSeconds(15));
                return ServiceResult<User>.Fail("Пользователь не найден.", StatusCodes.Status404NotFound);
            }


;
            _cache.Set(cacheKey, user, TimeSpan.FromMinutes(3));
            _logger.LogInformation("Пользователь сохранен в кэше. Идентификатор запрошенного пользователя: {RequestedUserId}", id);
        }
      
        _logger.LogInformation(
            "Запрос данных пользователя завершен. Идентификатор текущего пользователя: {CurrentUserId}, идентификатор запрошенного пользователя: {RequestedUserId}",
            user!.Id,
            id);

#pragma warning disable CS8602 // Разыменование вероятной пустой ссылки.

       return ServiceResult<User>.Ok(user); 


    }
       public async Task<ServiceResult<User?>> GetCurrentUserAsync(ClaimsPrincipal user,CancellationToken token)
    {
        var currentUserId = user.FindFirstValue(ClaimTypes.NameIdentifier);

        if (string.IsNullOrWhiteSpace(currentUserId))
        {
            _logger.LogWarning("Идентификатор текущего пользователя не найден в утверждениях.");
            return ServiceResult<User?>.Fail("Пользователь не найден.", StatusCodes.Status401Unauthorized);
        }

        if (!int.TryParse(currentUserId, out var userId))
        {
            _logger.LogWarning("Идентификатор текущего пользователя в утверждениях имеет неверный формат. Идентификатор текущего пользователя: {CurrentUserId}", currentUserId);
            return ServiceResult<User?>.Fail("Пользователь не найден.", StatusCodes.Status401Unauthorized);
            
        }

       var cacheKey = CacheKeys.CurrentUserById(userId);
       var notFoundCacheKey = CacheKeys.CurrentUserNotFound(userId);

if (_cache.TryGetValue(notFoundCacheKey, out bool _))
{
    return ServiceResult<User?>.Fail("Пользователь не найден.", StatusCodes.Status401Unauthorized);
}

if (!_cache.TryGetValue(cacheKey, out User? currentUser))
{
    currentUser = await _context.Users
        .AsNoTracking()
        .FirstOrDefaultAsync(user => user.Id == userId,token);

    if (currentUser != null)
    {
        _cache.Set(cacheKey, currentUser, TimeSpan.FromSeconds(50));
    }
}

if (currentUser != null)
{
    return ServiceResult<User?>.Ok(currentUser);
}  
        _cache.Set(notFoundCacheKey, true, TimeSpan.FromSeconds(15));
        

        _logger.LogWarning("Текущий пользователь не найден в базе данных. Идентификатор пользователя у провайдера: {ProviderUserId}", currentUserId);
       return  ServiceResult<User?>.Fail("Пользователь не найден.", StatusCodes.Status401Unauthorized);
        
    }

    public async Task<ServiceResult<string>> LogoutAsync(ClaimsPrincipal user,CancellationToken token)
    {
        _logger.LogInformation("Запрос выхода из аккаунта начат.");

        var currentUser = await GetCurrentUserAsync(user,token);
        if (!currentUser.Success || currentUser.Data is null)
        {
            _logger.LogWarning("Выход не выполнен: текущий пользователь не найден.");
            return ServiceResult<string>.Fail("Требуется авторизация.", StatusCodes.Status401Unauthorized);
        }

        if (currentUser.Data.IsBlocked)
        {
            _logger.LogWarning("Выход запрещен: пользователь заблокирован. Идентификатор пользователя: {CurrentUserId}", currentUser.Data.Id);
            return ServiceResult<string>.Fail(BlockedUserMessage.Create(currentUser.Data), StatusCodes.Status403Forbidden);
        }

        var userToLogout = await _context.Users.FirstOrDefaultAsync(x => x.Id == currentUser.Data.Id);
        if (userToLogout != null)
        {
            userToLogout.RefreshTokenHash = null;
        }

        await _action.AddActionAsync(currentUser.Data, "выход из аккаунта",token);
        await _context.SaveChangesAsync();
        RemoveUserCache(currentUser.Data.Id);

        _logger.LogInformation("Запрос выхода из аккаунта завершен. Идентификатор текущего пользователя: {CurrentUserId}", currentUser.Data.Id);
        return ServiceResult<string>.Ok("Вы вышли из аккаунта.");
    }

    public async Task<ServiceResult<List<UserHistoryDto>>> GetUserHistoryAsync(ClaimsPrincipal user,CancellationToken token)
    {
        var currentUser = await GetCurrentUserAsync(user,token);
        if (!currentUser.Success || currentUser.Data is null)
        {
            return ServiceResult<List<UserHistoryDto>>.Fail("Требуется авторизация.", StatusCodes.Status401Unauthorized);
        }

        if (currentUser.Data.IsBlocked)
        {
            _logger.LogWarning("Получение истории запрещено: пользователь заблокирован. Идентификатор пользователя: {CurrentUserId}", currentUser.Data.Id);
            return ServiceResult<List<UserHistoryDto>>.Fail(BlockedUserMessage.Create(currentUser.Data), StatusCodes.Status403Forbidden);
        }

        var cacheKey = CacheKeys.UserHistory(currentUser.Data.Id);
        if (!_cache.TryGetValue(cacheKey, out List<UserHistoryDto>? actions))
        {
            var moscowZone = TimeZoneInfo.FindSystemTimeZoneById("Russian Standard Time");
            var histories = await _context.UserActionHistories
                .AsNoTracking()
                .Where(x => x.users_id == currentUser.Data.Id)
                .OrderBy(x => x.CreatedAt)
                .Select(x => new UserHistoryDto
                {
                    action = x.UserAction != null ? x.UserAction.Action : null,
                    time = x.CreatedAt
                })
                .ToListAsync(token);

            actions = histories.Select(x => new UserHistoryDto
            {
                action = x.action,
                time = TimeZoneInfo.ConvertTimeFromUtc(x.time, moscowZone)
            }).ToList();

            _cache.Set(cacheKey, actions, TimeSpan.FromSeconds(100));
        }

        return ServiceResult<List<UserHistoryDto>>.Ok(actions ?? new List<UserHistoryDto>());
    }

  public async Task<ServiceResult<PaginationReult>> GetAllUsersAsync(PaginationParams pags,CancellationToken token ){
       var pageSize = 10;
           
           var users = await _context.Users
               .AsNoTracking()
               .OrderBy(x=>x.Id)
               .Skip((pags.Page- 1)*pageSize)
               .Take(pageSize)
               .ToListAsync(token);
        var countUsers =  await _context.Users.AsNoTracking().CountAsync(token);
       
            if (users == null)
            {
                return ServiceResult<PaginationReult>.Fail("Список пользователей не найден.", StatusCodes.Status404NotFound);
            }

            return ServiceResult<PaginationReult>.Ok(new PaginationReult
            {
                users = users,
                Page = pags.Page,
                PageSize = pageSize,
                TotalCount = countUsers
            });
        
  }


public async Task<ServiceResult<CurrentUserProfileDto>> GetCurrentUserProfileAsync(ClaimsPrincipal user,CancellationToken token){
 var currentUser = await GetCurrentUserAsync(user,token);
        if (currentUser.Data == null)
        {
            return ServiceResult<CurrentUserProfileDto>.Fail("Пользователь не найден.", StatusCodes.Status401Unauthorized);
        }
        if (user.Identity?.IsAuthenticated == true)
        {
            await _action.AddActionAsync(currentUser.Data, "данные о профиле",token);
       await _context.SaveChangesAsync(token);
            _logger.LogInformation("Запрос утверждений текущего пользователя завершен. Идентификатор текущего пользователя: {CurrentUserId}", currentUser.Data.Id);
            
         var User  = new CurrentUserProfileDto()
         {
             Login = currentUser.Data.Login,
             Role = "User"
         };
            return ServiceResult<CurrentUserProfileDto>.Ok(User);
        }
        return ServiceResult<CurrentUserProfileDto>.Fail("Пользователь не найден.", StatusCodes.Status401Unauthorized); 
}



 public async Task<ServiceResult<string>> RenameUserAsync(int id, string name,ClaimsPrincipal user,CancellationToken token){
     

        if (id <= 0)
        {
     return ServiceResult<string>.Fail("Некорректный id.", StatusCodes.Status400BadRequest);
            
        }

        if (string.IsNullOrWhiteSpace(name)||name==null)
        {
     return ServiceResult<string>.Fail("Некорректное имя.", StatusCodes.Status400BadRequest);
            
        }

        if (await _context.Users
            .AsNoTracking()
            .AnyAsync(user => user.Id != id && user.Login == name,token))
        {
     return ServiceResult<string>.Fail("Логин уже занят.", StatusCodes.Status409Conflict);
            
        }

        var userToRename = await _context.Users.FirstOrDefaultAsync(item => item.Id == id,token);
        if (userToRename == null)
        {
            return ServiceResult<string>.Fail("Пользователь не найден.", StatusCodes.Status404NotFound);
        }

        userToRename.Login = name;
        await _action.AddActionAsync(userToRename, "смена имени",token);
        RemoveUserCache(id);
        
        _logger.LogInformation("Запрос переименования пользователя завершен. Идентификатор текущего пользователя: {CurrentUserId}, идентификатор целевого пользователя: {TargetUserId}", userToRename.Id, id);
return ServiceResult<string>.Ok($"вы сменили имя на {name}");
 }
}
