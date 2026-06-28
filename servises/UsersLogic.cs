using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using MyApiBlya.Services;
using System.Security.Claims;

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
        
    
     public async Task<ServiceResult<User>> GetUserByIdAsync(int id)
    { 
        
        var cacheKey = CacheKeys.UserById(id);
        var notFoundCacheKey = CacheKeys.UserNotFound(id);

        if (_cache.TryGetValue(notFoundCacheKey, out bool _))
        {
            return ServiceResult<User>.Fail("Пользователь не найден.");
        }

        if (!_cache.TryGetValue(cacheKey, out User? user))
        {
            _logger.LogInformation("Пользователь не найден в кэше. Идентификатор запрошенного пользователя: {RequestedUserId}", id);
            user = await _context.Users.FirstOrDefaultAsync(x => x.Id == id);
            if (user == null)
            {
                _cache.Set(notFoundCacheKey, true, TimeSpan.FromSeconds(15));
                return ServiceResult<User>.Fail("Пользователь не найден.");
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
       public async Task<ServiceResult<User?>> GetCurrentUserAsync(ClaimsPrincipal user)
    {
        var currentUserId = user.FindFirstValue(ClaimTypes.NameIdentifier);

        if (string.IsNullOrWhiteSpace(currentUserId))
        {
            _logger.LogWarning("Идентификатор текущего пользователя не найден в утверждениях.");
            return ServiceResult<User?>.Fail("Пользователь не найден.");
        }

        if (!int.TryParse(currentUserId, out var userId))
        {
            _logger.LogWarning("Идентификатор текущего пользователя в утверждениях имеет неверный формат. Идентификатор текущего пользователя: {CurrentUserId}", currentUserId);
            return ServiceResult<User?>.Fail("Пользователь не найден.");
            
        }

       var cacheKey = CacheKeys.CurrentUserById(userId);
       var notFoundCacheKey = CacheKeys.CurrentUserNotFound(userId);

if (_cache.TryGetValue(notFoundCacheKey, out bool _))
{
    return ServiceResult<User?>.Fail("Пользователь не найден.");
}

if (!_cache.TryGetValue(cacheKey, out User? currentUser))
{
    currentUser = await _context.Users
        .AsNoTracking()
        .FirstOrDefaultAsync(user => user.Id == userId);

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
          return  ServiceResult<User?>.Fail("Пользователь не найден.");
        
    }

  public async Task<ServiceResult<List<User>>> GetAllUsersAsync(){
       
           
           var users = await _context.Users.ToListAsync();
        
       
            if (users == null)
            {
                ServiceResult<List<User>>.Fail("Список пользователей не найден.");
            }

               return  ServiceResult<List<User>>.Ok(users!);
        
  }


public async Task<ServiceResult<CurrentUserProfileDto>> GetCurrentUserProfileAsync(ClaimsPrincipal user){
 var currentUser = await GetCurrentUserAsync(user);
        if (currentUser.Data == null)
        {
            return ServiceResult<CurrentUserProfileDto>.Fail("Пользователь не найден.");
        }
        if (user.Identity?.IsAuthenticated == true)
        {
            await _action.AddActionAsync(currentUser.Data, "данные о профиле");
       
            _logger.LogInformation("Запрос утверждений текущего пользователя завершен. Идентификатор текущего пользователя: {CurrentUserId}", currentUser.Data.Id);
            
         var User  = new CurrentUserProfileDto()
         {
             Login = currentUser.Data.Login,
             Password= currentUser.Data.Password,
             Role = "User",
RefreshTokenHash = currentUser.Data.RefreshTokenHash
         };
            return ServiceResult<CurrentUserProfileDto>.Ok(User);
        }
        return ServiceResult<CurrentUserProfileDto>.Fail("Пользователь не найден."); 
}



 public async Task<ServiceResult<string>> RenameUserAsync(int id, string name,ClaimsPrincipal user){
     

        if (id <= 0)
        {
     return ServiceResult<string>.Fail("Некорректный id.");
            
        }

        if (string.IsNullOrWhiteSpace(name)||name==null)
        {
     return ServiceResult<string>.Fail("Некорректное имя.");
            
        }

        if (await _context.Users.AnyAsync(user => user.Id != id && user.Login == name))
        {
     return ServiceResult<string>.Fail("Логин уже занят.");
            
        }

        var userToRename = await _context.Users.FirstOrDefaultAsync(item => item.Id == id);
        if (userToRename == null)
        {
            return ServiceResult<string>.Fail("Пользователь не найден.");
        }

        userToRename.Login = name;
        await _action.AddActionAsync(userToRename, "смена имени");
        _cache.Remove(CacheKeys.UserById(id));
        _cache.Remove(CacheKeys.UserNotFound(id));
        _cache.Remove(CacheKeys.CurrentUserById(id));
        _cache.Remove(CacheKeys.CurrentUserNotFound(id));
    
        
        _logger.LogInformation("Запрос переименования пользователя завершен. Идентификатор текущего пользователя: {CurrentUserId}, идентификатор целевого пользователя: {TargetUserId}", userToRename.Id, id);
return ServiceResult<string>.Ok($"вы сменили имя на {name}");
 }
}


