    using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using System;
using MyApiBlya.Services;
public class GoogleUserService : IGoogleUserService {
    private readonly AppDbContext _context; 
    private readonly ILogger<GoogleUserService> _logger;
    private readonly INotificationService _email; 

public GoogleUserService(AppDbContext context, ILogger<GoogleUserService> logger, INotificationService email)
    {
        _context = context;
        _logger = logger;
        _email = email;
    }

    public async Task<User> FindOrCreateGoogleUserAsync(ClaimsPrincipal userClaims,CancellationToken token)
{
    var providerId = userClaims.FindFirst(ClaimTypes.NameIdentifier)?.Value;
    var email = userClaims.FindFirst(ClaimTypes.Email)?.Value;
    var name = userClaims.FindFirst(ClaimTypes.Name)?.Value;

    if (string.IsNullOrWhiteSpace(providerId))
    {
        _logger.LogWarning("Аутентификация Google не выполнена: отсутствует идентификатор пользователя у провайдера.");
        throw new Exception("Идентификатор пользователя Google не найден.");
    }

    var user = await _context.Users
        .FirstOrDefaultAsync(x =>
            x.Provider == "Google" &&
            x.ProviderUserId == providerId,token);

    if (user is null)
    {
        user = new User
        {
            Login = name,
            Password = "",
            Email = email,
            Provider = "Google",
            ProviderUserId = providerId,
            Role = "User",
            CreatedAt = DateTime.UtcNow,
            RefreshTokenExpiresAt = DateTime.UtcNow
        };
            
            
                
            

      await _context.Users.AddAsync(user,token);
        await _context.SaveChangesAsync(token);
        _logger.LogInformation("Создан пользователь через Google. Идентификатор пользователя: {UserId}", user.Id);
    }
await _email.SendAsync("sarkicyn@icloud.com","добро пожаловать!","вы успешно прошли аутентификацию",token);
    return user;
}}


