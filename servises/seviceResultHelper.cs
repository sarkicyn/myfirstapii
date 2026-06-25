using Microsoft.AspNetCore.Mvc;
using MyApiBlya.Services;
public static class Servicehelper
{
    public static IActionResult ToActionRes<T>(ControllerBase controller,ServiceResult<T> result)
    {
        if (result.Success)
        {
            return controller.Ok(result.Data);
        }
        return controller.BadRequest(result.Error); 
    }
}