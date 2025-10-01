using System.Diagnostics;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Api.Filters;

public class LogActionFilter : IAsyncActionFilter
{
    private readonly ILogger<LogActionFilter> _logger;

    public LogActionFilter(ILogger<LogActionFilter> logger)
    {
        _logger = logger;
    }

    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var action = $"{context.RouteData.Values["controller"]}.{context.RouteData.Values["action"]}";
        
        _logger.LogInformation("Entered: {Action}", action);
        
        var sw = Stopwatch.StartNew();
        await next();
        sw.Stop();
        
        _logger.LogInformation("Left: {Action} ({ElapsedMs}ms)", action, sw.ElapsedMilliseconds);
    }
}
