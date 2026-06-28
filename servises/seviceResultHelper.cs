using Microsoft.AspNetCore.Mvc;
using MyApiBlya.Services;
public static class ServiceResultMapper
{
    public static IActionResult ToActionResult<T>(ControllerBase controller,ServiceResult<T> result)
    {
        if (result.Success)
        {
            return controller.Ok(result.Data);
        }
        return controller.BadRequest(result.Error); 
    }
}
