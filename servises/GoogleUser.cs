    using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using System;
using MyApiBlya.Services;
public class Google : IGoogl {
    private readonly AppDbContext _context; 
    private readonly ILogger<Google> _logger;

public Google(AppDbContext context, ILogger<Google> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<User> GoogleLogin(ClaimsPrincipal userClaims)
{
    var providerId = userClaims.FindFirst(ClaimTypes.NameIdentifier)?.Value;
    var email = userClaims.FindFirst(ClaimTypes.Email)?.Value;
    var name = userClaims.FindFirst(ClaimTypes.Name)?.Value;

    if (string.IsNullOrWhiteSpace(providerId))
    {
        _logger.LogWarning("Google authentication failed: provider user id is missing.");
        throw new Exception("Идентификатор пользователя Google не найден.");
    }

    var user = await _context.users
        .FirstOrDefaultAsync(x =>
            x.Provider == "Google" &&
            x.ProviderUserId == providerId);

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
            
            
                
            

      await _context.users.AddAsync(user);
        await _context.SaveChangesAsync();
        _logger.LogInformation("Создан пользователь через Google. Идентификатор пользователя: {UserId}", user.Id);
    }

    return user;
}}
