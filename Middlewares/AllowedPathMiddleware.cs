using Microsoft.AspNetCore.Http;
using Microsoft.VisualBasic;

public class AllowedPathMiddleware 
{
    private readonly RequestDelegate _next;
    private readonly ILogger<AllowedPathMiddleware> _logger;
    public AllowedPathMiddleware(RequestDelegate next, ILogger<AllowedPathMiddleware> logger)
    {
        _next = next;/// сохраняем делегат
        _logger = logger;
    }

public async Task InvokeAsync(HttpContext context)
    {

        var path = context.Request.Path.ToString();
        var pathes = new[]
{
  
    "/api/users",
    "/google-path",
    "/github-path",
    "/swagger",
    "/health",
    "/metrics"
    

};

var allowed = pathes.Any(item =>
    path.StartsWith(item, StringComparison.OrdinalIgnoreCase));
    if (!allowed)
    {
        _logger.LogWarning(
            "Запрещенный путь запроса. Method: {Method}, Path: {Path}, IP: {Ip}",
            context.Request.Method,
            context.Request.Path,
            context.Connection.RemoteIpAddress?.ToString() ?? "unknown");
        context.Response.StatusCode = 403; 
        await context.Response.WriteAsJsonAsync(new { message = "Доступ к этому пути запрещен." }); 

        return;
    }
     
      
        
        await _next(context);//передаем в делегат обработанный  в классе контекст и передаем сам делегат в след.middleware
     
   
       
    }
}

