using Microsoft.AspNetCore.Mvc;

namespace Donately.Infrastructure.Middleware;

public class GlobalExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<GlobalExceptionMiddleware> _logger;
    private readonly IHostEnvironment _environment;

    public GlobalExceptionMiddleware(
        RequestDelegate next,
        ILogger<GlobalExceptionMiddleware> logger,
        IHostEnvironment environment)
    {
        _next = next;
        _logger = logger;
        _environment = environment;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception exception)
        {
            if (context.Response.HasStarted)
            {
                throw;
            }

            _logger.LogError(
                exception,
                "Unhandled exception while processing {Method} {Path}",
                context.Request.Method,
                context.Request.Path);

            context.Response.Clear();

            if (WantsJson(context.Request))
            {
                context.Response.StatusCode = StatusCodes.Status500InternalServerError;
                context.Response.ContentType = "application/problem+json";

                var problemDetails = new ProblemDetails
                {
                    Status = StatusCodes.Status500InternalServerError,
                    Title = "Сталася внутрішня помилка сервера",
                    Detail = _environment.IsDevelopment() ? exception.ToString() : null,
                    Instance = context.Request.Path
                };

                await context.Response.WriteAsJsonAsync(problemDetails);
                return;
            }

            if (context.Request.Path.StartsWithSegments("/Home/Error", StringComparison.OrdinalIgnoreCase))
            {
                context.Response.StatusCode = StatusCodes.Status500InternalServerError;
                context.Response.ContentType = "text/plain; charset=utf-8";
                await context.Response.WriteAsync("Сталася непередбачувана помилка.");
                return;
            }

            context.Response.Redirect("/Home/Error");
        }
    }

    private static bool WantsJson(HttpRequest request)
    {
        if (request.Path.StartsWithSegments("/api", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        return request.Headers.Accept.Any(header =>
            header?.Contains("application/json", StringComparison.OrdinalIgnoreCase) == true ||
            header?.Contains("application/problem+json", StringComparison.OrdinalIgnoreCase) == true);
    }
}


