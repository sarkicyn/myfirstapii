using Microsoft.AspNetCore.Mvc;
using MyApiBlya.Services;
using Microsoft.AspNetCore.Http;
public static class     ServiceResultMapper
{
    public static IActionResult ToActionResult<T>(ControllerBase controller,ServiceResult<T> result)
    {
        if (result.Success)
        {
            return controller.Ok(result.Data);
        }
        return result.StatusCode switch
        {
            StatusCodes.Status400BadRequest => controller.BadRequest(result.Error),
            StatusCodes.Status401Unauthorized => controller.Unauthorized(result.Error),
            StatusCodes.Status404NotFound => controller.NotFound(result.Error),
            StatusCodes.Status409Conflict => controller.Conflict(result.Error),
            _ => controller.StatusCode(result.StatusCode, result.Error)
        }; 
    }
}
