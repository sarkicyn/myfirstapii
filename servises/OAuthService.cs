using Microsoft.AspNetCore.Authentication;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using MyApiBlya.Services;

public class OAuthService : IOAuthService
{
     private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly AppDbContext _context;
 private readonly IMemoryCache _cache;
    private readonly ILogger<OAuthService> _logger;
    private readonly IAddAction _action;
    private readonly IJwtCreate _jwt;
private readonly IRefreshing _fresh; 
private readonly IGoogl _google;
private readonly IGitHubing _git;

    public OAuthService(AppDbContext context,IMemoryCache cache,ILogger<OAuthService> logger,IAddAction action,IJwtCreate jwt,IRefreshing fresh,IGoogl google,IGitHubing git,IHttpContextAccessor httpContextAccessor)
    {
        _context  = context;
        _cache = cache;
        _logger = logger; 
        _action = action;
        _jwt =jwt;
        _fresh  =fresh; 
        _google = google; 
        _git = git;     
        _httpContextAccessor = httpContextAccessor;
    }

    private void RemoveUserCache(int id)
    {
        _cache.Remove(CacheKeys.UserById(id));
        _cache.Remove(CacheKeys.UserNotFound(id));
        _cache.Remove(CacheKeys.CurrentUserById(id));
        _cache.Remove(CacheKeys.CurrentUserNotFound(id));
    }

  public async Task<ServiceResult<LoginResponse>> HandleGoogleCallback(){
    var authResult = await _httpContextAccessor.HttpContext!.AuthenticateAsync("Sexcheme");
if(!authResult.Succeeded||authResult.Principal is null)
        {
            return ServiceResult<LoginResponse>.Fail("Не удалось выполнить аутентификацию через Google.");
        }
   var result = await _google.GoogleLogin(authResult.Principal);
if (result.IsBlocked)
        {

            return ServiceResult<LoginResponse>.Fail("account_blocked");
        }
        
    var (refreshToken, hash) = _fresh.GenerateRefreshToken();
_logger.LogInformation("Сгенерирован refresh token для входа через Google.");
await _fresh.SaveRefreshTokenAsync(result,hash);
        

        result.RefreshTokenHash = hash;
      var jwt = await _jwt.GenerateToken(result);
        await _context.SaveChangesAsync();
        RemoveUserCache(result.Id);
await _action.AddActions(result, "вход через google");
await _httpContextAccessor.HttpContext!.SignOutAsync("Sexcheme");
       
    return ServiceResult<LoginResponse>.Ok(new LoginResponse{
        Refresh = refreshToken,
       Jwt =  jwt});
}
  public async Task<ServiceResult<LoginResponse>> HandleGitHubCallback(){
  var authResult = await _httpContextAccessor.HttpContext!.AuthenticateAsync("Sexcheme");
if(!authResult.Succeeded||authResult.Principal is null)
        {
            return ServiceResult<LoginResponse>.Fail("Не удалось выполнить аутентификацию через GitHub.");
        }
   var result = await _git.GithubLogin(authResult.Principal);
if (result.IsBlocked)
        {
            
            return ServiceResult<LoginResponse>.Fail("account_blocked");
        }
        
    var (refreshToken, hash) = _fresh.GenerateRefreshToken();
_logger.LogInformation("Сгенерирован refresh token для входа через GitHub.");
await _fresh.SaveRefreshTokenAsync(result,hash);
        
        

      var jwt = await _jwt.GenerateToken(result);
        await _context.SaveChangesAsync();
        RemoveUserCache(result.Id);
await _action.AddActions(result, "вход через github");
await _httpContextAccessor.HttpContext!.SignOutAsync("Sexcheme");
       
    return ServiceResult<LoginResponse>.Ok(new LoginResponse{
        Refresh = refreshToken,
       Jwt =  jwt});

}
}
