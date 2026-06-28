using Microsoft.AspNetCore.Http;
using System.Diagnostics;

public class RequestLoggingMiddleware
{private readonly RequestDelegate _next;
    private readonly ILogger<RequestLoggingMiddleware> _logger;

    public  RequestLoggingMiddleware(RequestDelegate next, ILogger<RequestLoggingMiddleware> logger)
    {
    _next = next;
    _logger = logger;
    }
    public async Task InvokeAsync(HttpContext context)
    {
         var ip = context.Connection.RemoteIpAddress?.ToString()??"unknown";
    var path=context.Request.Path;
    var method = context.Request.Method;
    var stopwatch = Stopwatch.StartNew();

await _next(context);
stopwatch.Stop();

_logger.LogInformation(
    "HTTP request completed. IP: {Ip}, duration: {ElapsedMilliseconds} ms, method: {Method}, path: {Path}, status code: {StatusCode}",
    ip,
    stopwatch.ElapsedMilliseconds,
    method,
    path,
    context.Response.StatusCode);
    }
}

