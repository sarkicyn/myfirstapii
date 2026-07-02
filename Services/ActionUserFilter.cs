using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using MyApiBlya.Services; 
public  class ActiveUserFilter : IAsyncActionFilter
{
    private readonly IUserService _currentUserService;
    private readonly ILogger<ActiveUserFilter> _logger;

    public ActiveUserFilter(
        IUserService currentUserService,
        ILogger<ActiveUserFilter> logger)
    {
        _currentUserService = currentUserService;
        _logger = logger;
    }

    public async Task OnActionExecutionAsync(
        ActionExecutingContext context,
        ActionExecutionDelegate next)
    {
        var currentUser = await _currentUserService.GetCurrentUserAsync(context.HttpContext.User);

        if (!currentUser.Success || currentUser.Data is null)
        {
            _logger.LogWarning("Неавторизованный запрос.");
            context.Result = new UnauthorizedObjectResult(new { message = "Требуется авторизация." });
            return;
        }

        if (currentUser.Data.IsBlocked)
        {
            _logger.LogWarning(
                "Действие запрещено: текущий пользователь заблокирован. Идентификатор пользователя: {CurrentUserId}",
                currentUser.Data.Id);

            var result = new ObjectResult(new{message = "доступ запрещен"});
            result.StatusCode = StatusCodes.Status403Forbidden; 
            context.Result = result;

            return;
        }

        await next();
    }
}
