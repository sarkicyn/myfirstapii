using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using MyApiBlya.Services;
public class User1
{

    
    public string? Login { get; set; } 
    public string? Password { get; set; } = ""; 
    public string? Role {get;set;}  
    public string? RefreshTokenHash {get;set;}

public  List<string>actions = new List<string>();
    public static List<string> Rules = new List<string>{
   "user.Read","user.Profile","user.Auth"
       
};
}
public class LoginResponse
{
    public string Jwt { get; set; } = string.Empty;
    public string Refresh { get; set; } = string.Empty;
}


public interface HashPassword
{
    string HashPass(LoginDTO dto);
}
public interface IJwtCreate
{
    Task<string> GenerateToken(User user);
      Task<string> GenerateToken1(User user);
}
public interface IAddAction  
{
    Task AddActions(User user,string act);
}
public interface IRefreshing
{
     (string RefreshToken, string Hash)GenerateRefreshToken(); 
     Task SaveRefreshTokenAsync(User user, string hash);
     
}
public interface IGoogl
{
    Task<User> GoogleLogin(ClaimsPrincipal userClaims); 
}
public interface IGitHubing
{
        public  Task<User> GithubLogin(ClaimsPrincipal claims);
}
public interface IUserService
{
     public  Task< ServiceResult<User>> GetOneUser(int id);
     public  Task<ServiceResult<User?>> GetCurrentUserFromDatabaseAsync(ClaimsPrincipal user);
     public  Task <ServiceResult<List<User>>> GetAllUsers();
       public  Task<ServiceResult<User1>> me(ClaimsPrincipal user);
        public  Task<ServiceResult<string>> Rename(int id, string name,ClaimsPrincipal user);
}

public interface IAuthService
{
       public  Task<ServiceResult<LoginResponse>>Login(LoginDTO dTO);
        public  Task<ServiceResult<string>>Refresh( RefreshRequest request);
         public Task<ServiceResult<LoginResponse>> AdminAuth(LoginDTO dto);
}

public interface IOAuthService
{
      public Task <ServiceResult<LoginResponse>> HandleGoogleCallback();
      public  Task<ServiceResult<LoginResponse>> HandleGitHubCallback();
}
