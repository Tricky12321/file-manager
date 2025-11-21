using System;
using System.IO;
using System.Net.WebSockets;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace FileManager;

public class RequestMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<RequestMiddleware> _logger;

    public RequestMiddleware(RequestDelegate next, ILogger<RequestMiddleware> logger)
    {
        _logger = logger;
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Disable caching for API endpoints
        if (context.Request.Path.StartsWithSegments("/api"))
        {
            context.Response.Headers["Cache-Control"] = "no-store, no-cache, must-revalidate, max-age=0";
            context.Response.Headers["Pragma"] = "no-cache";
            context.Response.Headers["Expires"] = "-1";
        }

        try
        {
            await _next(context);
        }
        catch (WebSocketException e)
        {
            // Ignore websocket exceptions
        }
        catch (AggregateException e)
        {
            // Ignore websocket exceptions
        }
        catch (IOException e)
        {
            // Handle IOException separately, often related to client disconnects
        }
        catch (Exception e)
        {
            _logger.LogError(e, "{Name} | {Method}", GetType().Name, MethodBase.GetCurrentMethod()?.DeclaringType?.Name);
            context.Response.ContentType = "JSON";
            context.Response.StatusCode = StatusCodes.Status500InternalServerError;
            await context.Response.WriteAsync("");
        }
    }
}