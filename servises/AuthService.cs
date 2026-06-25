using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using MyApiBlya.Services;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

public class AuthService : IAuthService
{
    private readonly AppDbContext _context;
 private readonly IMemoryCache _cache;
    private readonly ILogger<AuthService> _logger;
    private readonly IAddAction _action;
    private readonly IJwtCreate _jwt;
private readonly IRefreshing _fresh; 
private readonly IConfiguration _conf;
private readonly HashPassword _hashpass;

    public AuthService(AppDbContext context,IMemoryCache cache,ILogger<AuthService> logger,IAddAction action,IJwtCreate jwt,IRefreshing fresh,IConfiguration conf,HashPassword hashPass)
    {
        _context  = context;
        _cache = cache;
        _logger = logger; 
        _action = action;
        _jwt =jwt;
        _fresh  =fresh; 
        _conf =conf;
        _hashpass = hashPass; 
    }

    private void RemoveUserCache(int id)
    {
        _cache.Remove(CacheKeys.UserById(id));
        _cache.Remove(CacheKeys.UserNotFound(id));
        _cache.Remove(CacheKeys.CurrentUserById(id));
        _cache.Remove(CacheKeys.CurrentUserNotFound(id));
    }

     public async Task<ServiceResult<LoginResponse>>Login(LoginDTO dTO){
    if (dTO is null)
            {
     return ServiceResult<LoginResponse>.Fail("Данные отсутствуют.");
            }

            if (string.IsNullOrWhiteSpace(dTO.login) || string.IsNullOrWhiteSpace(dTO.password))
            {
     return ServiceResult<LoginResponse>.Fail("Некорректные данные.");
                   
            }
            if (dTO.login.Length <= 3 || dTO.password.Length <= 3)
        {
     return ServiceResult<LoginResponse>.Fail("Некорректные данные.");
            
        }
bool regg = Regex.IsMatch(dTO.login!, @"[^a-zA-Z0-9]");
        if (regg)
        {
     return ServiceResult<LoginResponse>.Fail("Некорректные данные.");
            
        }

            if (await _context.users.AnyAsync(user => user.Login == dTO.login))
            {
     return ServiceResult<LoginResponse>.Fail("Логин уже занят.");
            
            }
var (refreshToken, hash) = _fresh.GenerateRefreshToken();
_logger.LogInformation("Сгенерирован refresh token для входа пользователя."); 
var us = await _context.users.FirstOrDefaultAsync(x=>x.Login==dTO.login&&x.Password==dTO.password); 
if(us==null){ 
    var hashPass =  _hashpass.HashPass(dTO); 
         us = new User()
        {  
            Login = dTO.login,
            Password = hashPass,
            Role = "User",
            CreatedAt = DateTime.UtcNow
        };}
await _fresh.SaveRefreshTokenAsync(us,hash);
        await _context.users.AddAsync(us);
        await _context.SaveChangesAsync();

      var jwt = await _jwt.GenerateToken(us);
        await _context.SaveChangesAsync();
        RemoveUserCache(us.Id);
        await _action.AddActions(us, "вход пользователя");
       
        return ServiceResult<LoginResponse>.Ok(new LoginResponse
{
    Jwt = jwt,
    Refresh = refreshToken
}); 
 }

   public async Task<ServiceResult<string>> Refresh( RefreshRequest request){
 var refToken =  request.Refresh;

       if (string.IsNullOrWhiteSpace(refToken))
{
     return ServiceResult<string>.Fail("Токен отсутствует.");
    
}

       var token = Convert.ToBase64String(
            SHA256.HashData(
                Encoding.UTF8.GetBytes(refToken)));
        var userTrue = await _context.users.FirstOrDefaultAsync(x =>
            x.RefreshTokenHash == token ||
            x.RefreshTokenHash == refToken);
       if (userTrue is null)
{
     return ServiceResult<string>.Fail("Пользователь не найден.");
   
}

if (userTrue.RefreshTokenExpiresAt <= DateTime.UtcNow)
{
     return ServiceResult<string>.Fail("Срок действия токена истек. Выполните вход заново.");
    
    
}
var jwt  = await _jwt.GenerateToken1(userTrue);
await _action.AddActions(userTrue, "обновление jwt токена");
return ServiceResult<string>.Ok(jwt);
   }

    public async Task<ServiceResult<LoginResponse>> AdminAuth(LoginDTO dto)
    {
        if (dto is null)
        {
            return ServiceResult<LoginResponse>.Fail("Invalid data.");
        }

          var adminLogin = _conf["ADMIN_LOGIN"];
var adminPassword = _conf["ADMIN_PASSWORD"];
        if (dto.login == adminLogin && dto.password == adminPassword)
        {
            
        
          var user = await _context.users.FirstOrDefaultAsync(x=>x.Login ==dto.login&&x.Password==dto.password);
        if (user == null)
        {

            user = new User()
            {
                Login = dto.login,
                Password = dto.password,
                Role = "Admin",
                IsBlocked = false,
                CreatedAt = DateTime.UtcNow
                
            };
            await _context.users.AddAsync(user); 
    }
             var(refreshToken,hash) =  _fresh.GenerateRefreshToken();
             await _fresh.SaveRefreshTokenAsync(user,hash); 
              await _context.SaveChangesAsync();
              var jwt = await _jwt.GenerateToken1(user);
              await _action.AddActions(user, "вход администратора");
              RemoveUserCache(user.Id);
    return ServiceResult<LoginResponse>.Ok(new LoginResponse{Refresh=refreshToken,Jwt = jwt});
        }
    return ServiceResult<LoginResponse>.Fail("Неверные данные.");
    }
}
