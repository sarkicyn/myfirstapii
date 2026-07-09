using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using MyApiBlya.Services;
using System.Security.Principal;
public class    CurrentUserProfileDto
{

    
    public string? Login { get; set; } 
    public string? Role {get;set;}  
    

public  List<string>actions = new List<string>();
    public static List<string> Rules = new List<string>{
   "user.Read","user.Profile","user.Auth"
       
};
}
public class PaginationParams()
{
    public int Page {get;set;} = 1 ; 



}

public class LoginResponse
{
    public string Jwt { get; set; } = string.Empty;
    public string RefreshToken { get; set; } = string.Empty;
}


public interface IPasswordHashService
{
    string HashPassword(LoginDto dto);
}
public interface IJwtTokenService
{
    Task<string> GenerateUserTokenAsync(User user);
      Task<string> GenerateAdminTokenAsync(User user);
}
public interface IUserActionService  
{
    Task AddActionAsync(User user,string act,CancellationToken token);
}
public interface IRefreshTokenService
{
     (string RefreshToken, string Hash) GenerateRefreshToken(); 
     Task SaveRefreshTokenAsync(User user, string hash,CancellationToken token);
     
}
public interface IGoogleUserService
{
    Task<User> FindOrCreateGoogleUserAsync(ClaimsPrincipal userClaims,CancellationToken token); 
}
public interface IGitHubUserService
{
        public  Task<User> FindOrCreateGitHubUserAsync(ClaimsPrincipal claims,CancellationToken token);
}
public interface    IUserService
{
     public  Task< ServiceResult<User>> GetUserByIdAsync(int id,CancellationToken token);
     public  Task<ServiceResult<User?>> GetCurrentUserAsync(ClaimsPrincipal user,CancellationToken token);
     public Task<ServiceResult<string>> LogoutAsync(ClaimsPrincipal user,CancellationToken token);
     public Task<ServiceResult<List<UserHistoryDto>>> GetUserHistoryAsync(ClaimsPrincipal user,CancellationToken token);
     public  Task <ServiceResult<PaginationReult>> GetAllUsersAsync(PaginationParams pagination,CancellationToken token);
       public  Task<ServiceResult<CurrentUserProfileDto>> GetCurrentUserProfileAsync(ClaimsPrincipal user,CancellationToken token);
        public  Task<ServiceResult<string>> RenameUserAsync(int id, string name,ClaimsPrincipal user,CancellationToken token);
}

public interface IAuthService
{
       public  Task<ServiceResult<LoginResponse>>LoginAsync(LoginDto dTO,CancellationToken token);
       public  Task<ServiceResult<LoginResponse>>RegisterAsync(LoginDto dTO,CancellationToken token);
        public  Task<ServiceResult<LoginResponse>>RefreshAllTokens(RefreshRequest request,CancellationToken token);
         public Task<ServiceResult<LoginResponse>> AuthenticateAdminAsync(LoginDto dto,CancellationToken token);
}

public interface IOAuthService
{
      public Task <ServiceResult<LoginResponse>> HandleGoogleCallback(CancellationToken token);
      public  Task<ServiceResult<LoginResponse>> HandleGitHubCallback(CancellationToken token);
}
public interface INotificationService
{
 public Task SendAsync(string to, string subject, string message,CancellationToken token);
}