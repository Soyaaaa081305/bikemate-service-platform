namespace BikeMate.Api.Middleware;

public sealed class ErrorHandlingMiddleware(RequestDelegate next, ILogger<ErrorHandlingMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (UnauthorizedAccessException ex)
        {
            logger.LogWarning(ex, "Unauthorized access");
            await WriteErrorAsync(context, StatusCodes.Status403Forbidden, ex.Message);
        }
        catch (KeyNotFoundException ex)
        {
            logger.LogWarning(ex, "Resource not found");
            await WriteErrorAsync(context, StatusCodes.Status404NotFound, ex.Message);
        }
        catch (ArgumentException ex)
        {
            logger.LogWarning(ex, "Invalid argument");
            await WriteErrorAsync(context, StatusCodes.Status400BadRequest, ex.Message);
        }
        catch (InvalidOperationException ex)
        {
            logger.LogWarning(ex, "Invalid operation");
            await WriteErrorAsync(context, StatusCodes.Status400BadRequest, ex.Message);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unhandled API error");
            await WriteErrorAsync(context, StatusCodes.Status500InternalServerError, "An unexpected server error occurred.");
        }
    }

    private static async Task WriteErrorAsync(HttpContext context, int statusCode, string message)
    {
        if (context.Response.HasStarted)
        {
            return;
        }

        context.Response.StatusCode = statusCode;
        context.Response.ContentType = "application/json";
        await context.Response.WriteAsJsonAsync(new { error = message });
    }
}
