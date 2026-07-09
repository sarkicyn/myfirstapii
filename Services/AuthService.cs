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
private readonly INotificationService _email;
    public AuthService(AppDbContext context,IMemoryCache cache,ILogger<AuthService> logger,IUserActionService action,IJwtTokenService jwt,IRefreshTokenService fresh,IConfiguration conf,IPasswordHashService HashPassword,INotificationService email)
    {
        _context  = context;
        _cache = cache;
        _logger = logger; 
        _action = action;
        _jwt =jwt;
        _fresh  =fresh; 
        _conf =conf;
        _hashpass = HashPassword; 
        _email = email;
    }

    private void RemoveUserCache(int id)
    {
        _cache.Remove(CacheKeys.UserById(id));
        _cache.Remove(CacheKeys.UserNotFound(id));
        _cache.Remove(CacheKeys.CurrentUserById(id));
        _cache.Remove(CacheKeys.CurrentUserNotFound(id));
    }

     public async Task<ServiceResult<LoginResponse>>LoginAsync(LoginDto dTO,CancellationToken token){
    if (dTO is null)
            {
     return ServiceResult<LoginResponse>.Fail("Данные отсутствуют.", StatusCodes.Status400BadRequest);
            }

            if (string.IsNullOrWhiteSpace(dTO.Login) || string.IsNullOrWhiteSpace(dTO.password))
            {
     return ServiceResult<LoginResponse>.Fail("Некорректные данные.", StatusCodes.Status400BadRequest);
                   
            }
            if (dTO.Login.Length <= 3 || dTO.password.Length <= 3)
        {
     return ServiceResult<LoginResponse>.Fail("Некорректные данные.", StatusCodes.Status400BadRequest);
            
        }
bool regg = Regex.IsMatch(dTO.Login!, @"[^a-zA-Z0-9]");
        if (regg)
        {
     return ServiceResult<LoginResponse>.Fail("Некорректные данные.", StatusCodes.Status400BadRequest);
            
        }

var (refreshToken, hash) = _fresh.GenerateRefreshToken();
_logger.LogInformation("Сгенерирован refresh token для входа пользователя."); 
var us = await _context.Users.FirstOrDefaultAsync(x=>x.Login ==dTO.Login,token); 
if(us==null){ 
     return ServiceResult<LoginResponse>.Fail("Неверный логин.", StatusCodes.Status401Unauthorized);
}
if(!BCrypt.Net.BCrypt.Verify(dTO.password, us.Password)){
     return ServiceResult<LoginResponse>.Fail("Неверный пароль.", StatusCodes.Status401Unauthorized);
}
if (us.IsBlocked)
{
     return ServiceResult<LoginResponse>.Fail(BlockedUserMessage.Create(us), StatusCodes.Status403Forbidden);
}
await _fresh.SaveRefreshTokenAsync(us,hash,token);
        await _context.SaveChangesAsync(token);

      var jwt = await _jwt.GenerateUserTokenAsync(us);
        await _context.SaveChangesAsync();
        RemoveUserCache(us.Id);
        await _action.AddActionAsync(us, "вход пользователя",token);
       
        return ServiceResult<LoginResponse>.Ok(new LoginResponse
{
    Jwt = jwt,
    RefreshToken = refreshToken
}); 
 }

     public async Task<ServiceResult<LoginResponse>>RegisterAsync(LoginDto dTO,CancellationToken token){
    if (dTO is null)
            {
     return ServiceResult<LoginResponse>.Fail("Данные отсутствуют.", StatusCodes.Status400BadRequest);
            }

            if (string.IsNullOrWhiteSpace(dTO.Login) || string.IsNullOrWhiteSpace(dTO.password))
            {
     return ServiceResult<LoginResponse>.Fail("Некорректные данные.", StatusCodes.Status400BadRequest);
                   
            }
            if (dTO.Login.Length <= 3 || dTO.password.Length <= 3)
        {
     return ServiceResult<LoginResponse>.Fail("Некорректные данные.", StatusCodes.Status400BadRequest);
            
        }
bool regg = Regex.IsMatch(dTO.Login!, @"[^a-zA-Z0-9]");
        if (regg)
        {
     return ServiceResult<LoginResponse>.Fail("Некорректные данные.", StatusCodes.Status400BadRequest);
            
        }

var us = await _context.Users
    .AsNoTracking()
    .FirstOrDefaultAsync(x=>x.Login ==dTO.Login,token); 
if(us!=null){ 
     return ServiceResult<LoginResponse>.Fail("Логин уже занят.", StatusCodes.Status409Conflict);
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
await _fresh.SaveRefreshTokenAsync(us,hash,token);
        await _context.Users.AddAsync(us,token);
        await _context.SaveChangesAsync(token);

      var jwt = await _jwt.GenerateUserTokenAsync(us);
        await _context.SaveChangesAsync();
        RemoveUserCache(us.Id);
        await _action.AddActionAsync(us, "регистрация пользователя",token);
await _email.SendAsync("sarkicyn@icloud.com","добро пожаловать!","вы успешно прошли аутентификацию",token);
        return ServiceResult<LoginResponse>.Ok(new LoginResponse
{
    Jwt = jwt,
    RefreshToken = refreshToken
}); 
 }

   public async Task<ServiceResult<LoginResponse>> RefreshAllTokens(RefreshRequest request,CancellationToken Token){
 var refToken =  request.RefreshToken;

       if (string.IsNullOrWhiteSpace(refToken))
{
     return ServiceResult<LoginResponse>.Fail("Токен отсутствует.", StatusCodes.Status400BadRequest);
    
}

       var token = Convert.ToBase64String(
            SHA256.HashData(
                Encoding.UTF8.GetBytes(refToken)));
        var userTrue = await _context.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(x =>
            x.RefreshTokenHash == token ||
            x.RefreshTokenHash == refToken,Token);
       if (userTrue is null)
{
     return ServiceResult<LoginResponse>.Fail("Пользователь не найден.", StatusCodes.Status401Unauthorized);
   
}

if (userTrue.RefreshTokenExpiresAt <= DateTime.UtcNow)
{
     return ServiceResult<LoginResponse>.Fail("Срок действия токена истек. Выполните вход заново.", StatusCodes.Status401Unauthorized);
    
    
}if(userTrue.Role=="Admin"){
var jwtAdmin  = await _jwt.GenerateAdminTokenAsync(userTrue);
var (Refresh,Hash)  = _fresh.GenerateRefreshToken();
await _fresh.SaveRefreshTokenAsync(userTrue,Hash,Token); 
await _action.AddActionAsync(userTrue, "обновление  токенов",Token);
return ServiceResult<LoginResponse>.Ok(new LoginResponse
{
    Jwt = jwtAdmin,
    RefreshToken = Refresh
});}

var jwt  = await _jwt.GenerateUserTokenAsync(userTrue);
var (refresh,hash) = _fresh.GenerateRefreshToken();
 await _fresh.SaveRefreshTokenAsync(userTrue,hash,Token);
await _action.AddActionAsync(userTrue, "обновление токенов",Token);
return ServiceResult<LoginResponse>.Ok(new LoginResponse
{
    Jwt = jwt,
    RefreshToken = refresh
});
   }

    public async Task<ServiceResult<LoginResponse>> AuthenticateAdminAsync(LoginDto dto,CancellationToken token)
    {
        if (dto is null)
        {
            return ServiceResult<LoginResponse>.Fail("Некорректные данные.", StatusCodes.Status400BadRequest);
        }

          var adminLogin = _conf["ADMIN_LOGIN"];
var adminPasswordHash = _conf["ADMIN_PASSWORD_HASH"];
if (string.IsNullOrWhiteSpace(adminLogin) ||
    string.IsNullOrWhiteSpace(adminPasswordHash))
{
    return ServiceResult<LoginResponse>.Fail("Неверные данные.", StatusCodes.Status500InternalServerError);
}

        if (dto.Login == adminLogin &&  BCrypt.Net.BCrypt.Verify(dto.password, adminPasswordHash))
        {
        
          var user = await _context.Users.FirstOrDefaultAsync(x=>x.Login == dto.Login,token);
        if (user == null)
        {
            user = new User()
            {
                Login = dto.Login,
                Password = adminPasswordHash,
                Role = "Admin",
                IsBlocked = false,
                CreatedAt = DateTime.UtcNow
                
            };
            await _context.Users.AddAsync(user); 
    }
             var(refreshToken,hash) =  _fresh.GenerateRefreshToken();
             await _fresh.SaveRefreshTokenAsync(user,hash,token); 
              await _context.SaveChangesAsync(token);
              var jwt = await _jwt.GenerateAdminTokenAsync(user);
              await _action.AddActionAsync(user, "вход администратора",token);
              RemoveUserCache(user.Id);
              
    return ServiceResult<LoginResponse>.Ok(new LoginResponse{RefreshToken =refreshToken,Jwt = jwt});
        }
    return ServiceResult<LoginResponse>.Fail("Неверные данные.", StatusCodes.Status401Unauthorized);
    }
}
