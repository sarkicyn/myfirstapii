using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using MyApiBlya.Services;

public class GitHubUserService : IGitHubUserService
{
    private readonly AppDbContext _context;
    private readonly ILogger<GitHubUserService> _logger;

    public GitHubUserService(AppDbContext context, ILogger<GitHubUserService> logger)
    {
        _context = context;
        _logger = logger;
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
            _logger.LogWarning("GitHub authentication failed: provider user id is missing.");
            throw new Exception("РРґРµРЅС‚РёС„РёРєР°С‚РѕСЂ РїРѕР»СЊР·РѕРІР°С‚РµР»СЏ GitHub РЅРµ РЅР°Р№РґРµРЅ.");
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
        _logger.LogInformation("РЎРѕР·РґР°РЅ РїРѕР»СЊР·РѕРІР°С‚РµР»СЊ С‡РµСЂРµР· GitHub. РРґРµРЅС‚РёС„РёРєР°С‚РѕСЂ РїРѕР»СЊР·РѕРІР°С‚РµР»СЏ: {UserId}", user.Id);

        return user;
    }
}


