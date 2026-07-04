using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using MyApiBlya.Services;
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
    Task AddActionAsync(User user,string act);
}
public interface IRefreshTokenService
{
     (string RefreshToken, string Hash) GenerateRefreshToken(); 
     Task SaveRefreshTokenAsync(User user, string hash);
     
}
public interface IGoogleUserService
{
    Task<User> FindOrCreateGoogleUserAsync(ClaimsPrincipal userClaims); 
}
public interface IGitHubUserService
{
        public  Task<User> FindOrCreateGitHubUserAsync(ClaimsPrincipal claims);
}
public interface IUserService
{
     public  Task< ServiceResult<User>> GetUserByIdAsync(int id);
     public  Task<ServiceResult<User?>> GetCurrentUserAsync(ClaimsPrincipal user);
     public Task<ServiceResult<string>> LogoutAsync(ClaimsPrincipal user);
     public Task<ServiceResult<List<UserHistoryDto>>> GetUserHistoryAsync(ClaimsPrincipal user);
     public  Task <ServiceResult<PaginationReult>> GetAllUsersAsync(PaginationParams pagination);
       public  Task<ServiceResult<CurrentUserProfileDto>> GetCurrentUserProfileAsync(ClaimsPrincipal user);
        public  Task<ServiceResult<string>> RenameUserAsync(int id, string name,ClaimsPrincipal user);
}

public interface IAuthService
{
       public  Task<ServiceResult<LoginResponse>>LoginAsync(LoginDto dTO);
       public  Task<ServiceResult<LoginResponse>>RegisterAsync(LoginDto dTO);
        public  Task<ServiceResult<LoginResponse>>RefreshAllTokens(RefreshRequest request);
         public Task<ServiceResult<LoginResponse>> AuthenticateAdminAsync(LoginDto dto);
}

public interface IOAuthService
{
      public Task <ServiceResult<LoginResponse>> HandleGoogleCallback();
      public  Task<ServiceResult<LoginResponse>> HandleGitHubCallback();
}
public interface INotificationService
{
 public Task SendAsync(string to, string subject, string message);
}