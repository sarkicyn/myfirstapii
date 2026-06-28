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
    private readonly IUserActionService _action;
    private readonly IJwtTokenService _jwt;
private readonly IRefreshTokenService _fresh; 
private readonly IGoogleUserService _google;
private readonly IGitHubUserService _git;

    public OAuthService(AppDbContext context,IMemoryCache cache,ILogger<OAuthService> logger,IUserActionService action,IJwtTokenService jwt,IRefreshTokenService fresh,IGoogleUserService google,IGitHubUserService git,IHttpContextAccessor httpContextAccessor)
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
            return ServiceResult<LoginResponse>.Fail("РќРµ СѓРґР°Р»РѕСЃСЊ РІС‹РїРѕР»РЅРёС‚СЊ Р°СѓС‚РµРЅС‚РёС„РёРєР°С†РёСЋ С‡РµСЂРµР· Google.");
        }
   var result = await _google.FindOrCreateGoogleUserAsync(authResult.Principal);
if (result.IsBlocked)
        {

            return ServiceResult<LoginResponse>.Fail("account_blocked");
        }
        
    var (refreshToken, hash) = _fresh.GenerateRefreshToken();
_logger.LogInformation("РЎРіРµРЅРµСЂРёСЂРѕРІР°РЅ refresh token РґР»СЏ РІС…РѕРґР° С‡РµСЂРµР· Google.");
await _fresh.SaveRefreshTokenAsync(result,hash);
        

        result.RefreshTokenHash = hash;
      var jwt = await _jwt.GenerateUserTokenAsync(result);
        await _context.SaveChangesAsync();
        RemoveUserCache(result.Id);
await _action.AddActionAsync(result, "РІС…РѕРґ С‡РµСЂРµР· google");
await _httpContextAccessor.HttpContext!.SignOutAsync("Sexcheme");
       
    return ServiceResult<LoginResponse>.Ok(new LoginResponse{
        RefreshToken = refreshToken,
       Jwt =  jwt});
}
  public async Task<ServiceResult<LoginResponse>> HandleGitHubCallback(){
  var authResult = await _httpContextAccessor.HttpContext!.AuthenticateAsync("Sexcheme");
if(!authResult.Succeeded||authResult.Principal is null)
        {
            return ServiceResult<LoginResponse>.Fail("РќРµ СѓРґР°Р»РѕСЃСЊ РІС‹РїРѕР»РЅРёС‚СЊ Р°СѓС‚РµРЅС‚РёС„РёРєР°С†РёСЋ С‡РµСЂРµР· GitHub.");
        }
   var result = await _git.FindOrCreateGitHubUserAsync(authResult.Principal);
if (result.IsBlocked)
        {
            
            return ServiceResult<LoginResponse>.Fail("account_blocked");
        }
        
    var (refreshToken, hash) = _fresh.GenerateRefreshToken();
_logger.LogInformation("РЎРіРµРЅРµСЂРёСЂРѕРІР°РЅ refresh token РґР»СЏ РІС…РѕРґР° С‡РµСЂРµР· GitHub.");
await _fresh.SaveRefreshTokenAsync(result,hash);
        
        

      var jwt = await _jwt.GenerateUserTokenAsync(result);
        await _context.SaveChangesAsync();
        RemoveUserCache(result.Id);
await _action.AddActionAsync(result, "РІС…РѕРґ С‡РµСЂРµР· github");
await _httpContextAccessor.HttpContext!.SignOutAsync("Sexcheme");
       
    return ServiceResult<LoginResponse>.Ok(new LoginResponse{
        RefreshToken = refreshToken,
       Jwt =  jwt});

}
}



