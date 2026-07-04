using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MyApiBlya.Services;

[ApiController]
[Route("api/users")]
public class CurrentUserController : ControllerBase
{
    private readonly IUserService _users;
    private readonly ILogger<CurrentUserController> _logger;
    private readonly IUserActionService _action;
    private readonly AppDbContext _context;

    public CurrentUserController(
        IUserActionService action,
        ILogger<CurrentUserController> logger,
        IUserService users,
        AppDbContext context)
    {
        _context = context;
        _logger = logger;
        _users = users;
        _action = action;
    }

    [Authorize]
    [ServiceFilter(typeof(ActiveUserFilter))]
    [HttpGet("me")]
    public async Task<IActionResult> GetCurrentUserProfileAsync()
    {
        var current = (await _users.GetCurrentUserAsync(User)).Data!;

        _logger.LogInformation("Запрос утверждений текущего пользователя начат.");

        var result = await _users.GetCurrentUserProfileAsync(User);

        if (result.Success)
        {
        

            return Ok(result.Data);
        }

        return Unauthorized(new { message = "Требуется авторизация." });
    }

    [Authorize]
    [ServiceFilter(typeof(ActiveUserFilter))]
    [HttpPut("rename")]
    public async Task<IActionResult> RenameUserAsync(string newLogin)
    {
        var current = (await _users.GetCurrentUserAsync(User)).Data!;
        var result = await _users.RenameUserAsync(current.Id, newLogin, User);

        if (result.Success)
        {
            return Ok(result.Data);
        }

        return ServiceResultMapper.ToActionResult(this, result);
    }

    [Authorize]
    [ServiceFilter(typeof(ActiveUserFilter))]
    [HttpGet("history")]
    public async Task<IActionResult> GetHistory()
    {
        var result = await _users.GetUserHistoryAsync(User);

        if (result.Success)
        {
            return Ok(result.Data ?? new List<UserHistoryDto>());
        }

        return ServiceResultMapper.ToActionResult(this, result);
    }
}
