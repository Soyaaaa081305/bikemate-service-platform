using System.Text.Json;
using BikeMate.Api.Middleware;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Moq;

namespace BikeMate.Tests.Middleware;

public sealed class ErrorHandlingMiddlewareTests
{
    private static ErrorHandlingMiddleware CreateMiddleware(RequestDelegate next)
    {
        var logger = new Mock<ILogger<ErrorHandlingMiddleware>>();
        return new ErrorHandlingMiddleware(next, logger.Object);
    }

    [Fact]
    public async Task InvokeAsync_PassesThroughOnSuccess()
    {
        var middleware = CreateMiddleware(_ => Task.CompletedTask);
        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();

        await middleware.InvokeAsync(context);

        Assert.Equal(200, context.Response.StatusCode);
    }

    [Fact]
    public async Task InvokeAsync_Returns403ForUnauthorizedAccessException()
    {
        var middleware = CreateMiddleware(_ => throw new UnauthorizedAccessException("Forbidden resource"));
        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();

        await middleware.InvokeAsync(context);

        Assert.Equal(403, context.Response.StatusCode);
        var body = await ReadResponseBody(context);
        Assert.Contains("Forbidden resource", body);
    }

    [Fact]
    public async Task InvokeAsync_Returns400ForInvalidOperationException()
    {
        var middleware = CreateMiddleware(_ => throw new InvalidOperationException("Bad request data"));
        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();

        await middleware.InvokeAsync(context);

        Assert.Equal(400, context.Response.StatusCode);
        var body = await ReadResponseBody(context);
        Assert.Contains("Bad request data", body);
    }

    [Fact]
    public async Task InvokeAsync_Returns500ForUnhandledException()
    {
        var middleware = CreateMiddleware(_ => throw new NullReferenceException("Unexpected"));
        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();

        await middleware.InvokeAsync(context);

        Assert.Equal(500, context.Response.StatusCode);
        var body = await ReadResponseBody(context);
        Assert.Contains("unexpected server error", body, StringComparison.OrdinalIgnoreCase);
        // Should NOT leak the internal error message
        Assert.DoesNotContain("Unexpected", body);
    }

    [Fact]
    public async Task InvokeAsync_ResponseBodyContainsJsonErrorField()
    {
        var middleware = CreateMiddleware(_ => throw new InvalidOperationException("Test error"));
        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();

        await middleware.InvokeAsync(context);

        var body = await ReadResponseBody(context);
        var json = JsonDocument.Parse(body);
        Assert.True(json.RootElement.TryGetProperty("error", out var errorProp));
        Assert.Equal("Test error", errorProp.GetString());
    }

    private static async Task<string> ReadResponseBody(HttpContext context)
    {
        context.Response.Body.Seek(0, SeekOrigin.Begin);
        using var reader = new StreamReader(context.Response.Body);
        return await reader.ReadToEndAsync();
    }
}
