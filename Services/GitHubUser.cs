using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using MyApiBlya.Services;

public class GitHubUserService : IGitHubUserService
{
    private readonly AppDbContext _context;
    private readonly ILogger<GitHubUserService> _logger;
private readonly INotificationService _email;
    public GitHubUserService(AppDbContext context, ILogger<GitHubUserService> logger,INotificationService email)
    {
        _context = context;
        _logger = logger;
        _email = email;
    }

    public async Task<User> FindOrCreateGitHubUserAsync(ClaimsPrincipal claims)
    {
        var githubId = claims
            .FindFirst(ClaimTypes.NameIdentifier)?.Value;

        var email = claims
            .FindFirst(ClaimTypes.Email)?.Value;

        var name = claims
            .FindFirst(ClaimTypes.Name)?.Value;

        if (string.IsNullOrWhiteSpace(githubId))
        {
            _logger.LogWarning("Аутентификация GitHub не выполнена: отсутствует идентификатор пользователя у провайдера.");
            throw new Exception("Идентификатор пользователя GitHub не найден.");
        }

        var user = await _context.Users
            .FirstOrDefaultAsync(x =>
                x.Provider == "GitHub" &&
                x.ProviderUserId == githubId);

        if (user is not null)
        {
            return user;
        }

        user = new User
        {
            Login = name,
            Password = "",
            Email = email,
            Provider = "GitHub",
            ProviderUserId = githubId,
            Role = "User",
            CreatedAt = DateTime.UtcNow,
            RefreshTokenExpiresAt = DateTime.UtcNow
        };

        await _context.Users.AddAsync(user);
        await _context.SaveChangesAsync();
        _logger.LogInformation("Создан пользователь через GitHub. Идентификатор пользователя: {UserId}", user.Id);
await _email.SendAsync("sarkicyn@icloud.com","добро пожаловать!","вы успешно прошли аутентификацию");

        return user;
    }
}


