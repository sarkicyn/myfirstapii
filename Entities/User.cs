namespace MyApiBlya.Services;

public class User
{
    public int Id { get; set; }

    public string? Login { get; set; }
    public string? Password { get; set; } = "";
    public string? Email { get; set; }
    public string? Role { get; set; }
    public string? RefreshTokenHash { get; set; }
    public string? Provider { get; set; }
    public string? ProviderUserId { get; set; }
    public DateTime CreatedAt { get; set; }
    public bool IsBlocked { get;set;} = false;
    public DateTime RefreshTokenExpiresAt { get; set; }

    public List<string> actions = new();
   


    public List<UserActionHistory> histories{get;set;} = new(); 

}


