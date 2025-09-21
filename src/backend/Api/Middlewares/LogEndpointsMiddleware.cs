using System.Diagnostics;

namespace Api.Middlewares;

public class LogEndpointsMiddleware 
{
    private readonly RequestDelegate _next;
    private readonly ILogger<LogEndpointsMiddleware> _logger;

    public LogEndpointsMiddleware(RequestDelegate next, ILogger<LogEndpointsMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        Stopwatch sw = new Stopwatch();

        _logger.LogInformation("HTTP {Method} {Path}{Query}",
                context.Request.Method,
                context.Request.Path,
                context.Request.QueryString);

        sw.Start();
        
        await _next(context);

        sw.Stop();

        _logger.LogInformation("HTTP {Method} {Path} responded {StatusCode} in {Elapsed}ms",
                context.Request.Method,
                context.Request.Path,
                context.Response.StatusCode,
                sw.ElapsedMilliseconds);
    }
}
