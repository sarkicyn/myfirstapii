using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

public  class CorrelationIdMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<CorrelationIdMiddleware> _logger;

    public CorrelationIdMiddleware(
        RequestDelegate next,
        ILogger<CorrelationIdMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var correlationId = Guid.NewGuid().ToString();

        using (_logger.BeginScope(new Dictionary<string, string>
        {
            ["CorrelationId"] = correlationId
        }))
        {
            await _next(context);
        }
    }
}