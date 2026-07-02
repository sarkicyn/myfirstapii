using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MyApiBlya.Services;

[ApiController]
[Route("api/users")]
[ServiceFilter(typeof(ActiveUserFilter))]
public class CurrentUserController : ControllerBase
{
    private readonly IUserService _users;
    private readonly ILogger<CurrentUserController> _logger;
    private readonly IUserActionService _action;
    private readonly AppDbContext _context;

    public CurrentUserController(IUserActionService action,ILogger<CurrentUserController> logger,
    IUserService users, AppDbContext context
    )
    {
        _context = context;
      _logger = logger;
      _users = users;
      _action = action;
    }


[Authorize]
    [HttpGet("me")]
    public async Task<IActionResult> GetCurrentUserProfileAsync()
    {

        var current = await _users.GetCurrentUserAsync(User);
        if (current.Success && current.Data is not null && current.Data.IsBlocked)
        {
            _logger.LogWarning("Получение профиля запрещено: пользователь заблокирован. Идентификатор пользователя: {CurrentUserId}", current.Data.Id);
            return StatusCode(StatusCodes.Status403Forbidden, new { message = "Действие запрещено: ваш аккаунт заблокирован." });
        }
        _logger.LogInformation("Запрос утверждений текущего пользователя начат.");

       var result = await _users.GetCurrentUserProfileAsync(User);

        if (result.Success)
        {
await _action.AddActionAsync(current.Data!,"получение данных о пользователе");
await _context.SaveChangesAsync();

            return Ok(result.Data);
        }

        return Unauthorized(new { message = "Требуется авторизация." });
    }

[Authorize]
    [HttpPut("rename")]
    public async Task<IActionResult> RenameUserAsync(string newLogin)
    {
        var current = await _users.GetCurrentUserAsync(User);
        if (current.Success && current.Data is not null && current.Data.IsBlocked)
        {
            _logger.LogWarning("Переименование запрещено: пользователь заблокирован. Идентификатор пользователя: {CurrentUserId}", current.Data.Id);
            return StatusCode(StatusCodes.Status403Forbidden, new { message = "Действие запрещено: ваш аккаунт заблокирован." });
        }
        var result  = await _users.RenameUserAsync(current.Data!.Id, newLogin, User);
        if (result.Success)
        {
            return Ok(result.Data);
        }

        return ServiceResultMapper.ToActionResult(this,result);


    }

    [Authorize]
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
