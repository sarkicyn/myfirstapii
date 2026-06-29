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
    private readonly IUserActionService _action;
    private readonly IJwtTokenService _jwt;
private readonly IRefreshTokenService _fresh; 
private readonly IConfiguration _conf;
private readonly IPasswordHashService _hashpass;

    public AuthService(AppDbContext context,IMemoryCache cache,ILogger<AuthService> logger,IUserActionService action,IJwtTokenService jwt,IRefreshTokenService fresh,IConfiguration conf,IPasswordHashService HashPassword)
    {
        _context  = context;
        _cache = cache;
        _logger = logger; 
        _action = action;
        _jwt =jwt;
        _fresh  =fresh; 
        _conf =conf;
        _hashpass = HashPassword; 
    }

    private void RemoveUserCache(int id)
    {
        _cache.Remove(CacheKeys.UserById(id));
        _cache.Remove(CacheKeys.UserNotFound(id));
        _cache.Remove(CacheKeys.CurrentUserById(id));
        _cache.Remove(CacheKeys.CurrentUserNotFound(id));
    }

     public async Task<ServiceResult<LoginResponse>>AuthenticateAsync(LoginDto dTO){
        return await LoginAsync(dTO);
 }

     public async Task<ServiceResult<LoginResponse>>LoginAsync(LoginDto dTO){
    if (dTO is null)
            {
     return ServiceResult<LoginResponse>.Fail("Данные отсутствуют.");
            }

            if (string.IsNullOrWhiteSpace(dTO.Login) || string.IsNullOrWhiteSpace(dTO.password))
            {
     return ServiceResult<LoginResponse>.Fail("Некорректные данные.");
                   
            }
            if (dTO.Login.Length <= 3 || dTO.password.Length <= 3)
        {
     return ServiceResult<LoginResponse>.Fail("Некорректные данные.");
            
        }
bool regg = Regex.IsMatch(dTO.Login!, @"[^a-zA-Z0-9]");
        if (regg)
        {
     return ServiceResult<LoginResponse>.Fail("Некорректные данные.");
            
        }

var (refreshToken, hash) = _fresh.GenerateRefreshToken();
_logger.LogInformation("Сгенерирован refresh token для входа пользователя."); 
var us = await _context.Users.FirstOrDefaultAsync(x=>x.Login ==dTO.Login); 
if(us==null){ 
     return ServiceResult<LoginResponse>.Fail("неверный логин.");
}
if(!BCrypt.Net.BCrypt.Verify(dTO.password, us.Password)){
     return ServiceResult<LoginResponse>.Fail("неверный пароль.");
}
await _fresh.SaveRefreshTokenAsync(us,hash);
        await _context.SaveChangesAsync();

      var jwt = await _jwt.GenerateUserTokenAsync(us);
        await _context.SaveChangesAsync();
        RemoveUserCache(us.Id);
        await _action.AddActionAsync(us, "вход пользователя");
       
        return ServiceResult<LoginResponse>.Ok(new LoginResponse
{
    Jwt = jwt,
    RefreshToken = refreshToken
}); 
 }

     public async Task<ServiceResult<LoginResponse>>RegisterAsync(LoginDto dTO){
    if (dTO is null)
            {
     return ServiceResult<LoginResponse>.Fail("Данные отсутствуют.");
            }

            if (string.IsNullOrWhiteSpace(dTO.Login) || string.IsNullOrWhiteSpace(dTO.password))
            {
     return ServiceResult<LoginResponse>.Fail("Некорректные данные.");
                   
            }
            if (dTO.Login.Length <= 3 || dTO.password.Length <= 3)
        {
     return ServiceResult<LoginResponse>.Fail("Некорректные данные.");
            
        }
bool regg = Regex.IsMatch(dTO.Login!, @"[^a-zA-Z0-9]");
        if (regg)
        {
     return ServiceResult<LoginResponse>.Fail("Некорректные данные.");
            
        }

var us = await _context.Users.FirstOrDefaultAsync(x=>x.Login ==dTO.Login); 
if(us!=null){ 
     return ServiceResult<LoginResponse>.Fail("Логин уже занят.");
}
var (refreshToken, hash) = _fresh.GenerateRefreshToken();
_logger.LogInformation("Сгенерирован refresh token для регистрации пользователя."); 
    var HashPassword =  _hashpass.HashPassword(dTO); 
         us = new User()
        {  
            Login = dTO.Login,
            Password = HashPassword,
            Role = "User",
            CreatedAt = DateTime.UtcNow
        };
await _fresh.SaveRefreshTokenAsync(us,hash);
        await _context.Users.AddAsync(us);
        await _context.SaveChangesAsync();

      var jwt = await _jwt.GenerateUserTokenAsync(us);
        await _context.SaveChangesAsync();
        RemoveUserCache(us.Id);
        await _action.AddActionAsync(us, "регистрация пользователя");
       
        return ServiceResult<LoginResponse>.Ok(new LoginResponse
{
    Jwt = jwt,
    RefreshToken = refreshToken
}); 
 }

   public async Task<ServiceResult<string>> RefreshJwtAsync(RefreshRequest request){
 var refToken =  request.RefreshToken;

       if (string.IsNullOrWhiteSpace(refToken))
{
     return ServiceResult<string>.Fail("Токен отсутствует.");
    
}

       var token = Convert.ToBase64String(
            SHA256.HashData(
                Encoding.UTF8.GetBytes(refToken)));
        var userTrue = await _context.Users.FirstOrDefaultAsync(x =>
            x.RefreshTokenHash == token ||
            x.RefreshTokenHash == refToken);
       if (userTrue is null)
{
     return ServiceResult<string>.Fail("Пользователь не найден.");
   
}

if (userTrue.RefreshTokenExpiresAt <= DateTime.UtcNow)
{
     return ServiceResult<string>.Fail("Срок действия токена истек. Выполните вход заново.");
    
    
}if(userTrue.Role=="Admin"){
var jwtAdmin  = await _jwt.GenerateAdminTokenAsync(userTrue);
await _action.AddActionAsync(userTrue, "обновление jwt токена");
return ServiceResult<string>.Ok(jwtAdmin);}

var jwt  = await _jwt.GenerateUserTokenAsync(userTrue);
await _action.AddActionAsync(userTrue, "обновление jwt токена");
return ServiceResult<string>.Ok(jwt);
   }

    public async Task<ServiceResult<LoginResponse>> AuthenticateAdminAsync(LoginDto dto)
    {
        if (dto is null)
        {
            return ServiceResult<LoginResponse>.Fail("Invalid data.");
        }

          var adminLogin = _conf["ADMIN_LOGIN"];
var adminPassword = _conf["ADMIN_PASSWORD"];
        if (dto.Login == adminLogin && dto.password == adminPassword)
        {
            
        
          var user = await _context.Users.FirstOrDefaultAsync(x=>x.Login == dto.Login);
        if (user == null)
        {
var HashPassword =  _hashpass.HashPassword(dto);
            user = new User()
            {
                Login = dto.Login,
                Password = HashPassword,
                Role = "Admin",
                IsBlocked = false,
                CreatedAt = DateTime.UtcNow
                
            };
            await _context.Users.AddAsync(user); 
    }
             var(refreshToken,hash) =  _fresh.GenerateRefreshToken();
             await _fresh.SaveRefreshTokenAsync(user,hash); 
              await _context.SaveChangesAsync();
              var jwt = await _jwt.GenerateAdminTokenAsync(user);
              await _action.AddActionAsync(user, "вход администратора");
              RemoveUserCache(user.Id);
    return ServiceResult<LoginResponse>.Ok(new LoginResponse{RefreshToken =refreshToken,Jwt = jwt});
        }
    return ServiceResult<LoginResponse>.Fail("Неверные данные.");
    }
}


