using System.Net;
using System.Text.Json;
using FluentValidation;

namespace Common.Middleware;

public class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An unhandled exception has occurred.");
            await HandleExceptionAsync(context, ex);
        }
    }

    private static Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        var code = HttpStatusCode.InternalServerError;
        var result = string.Empty;

        if (exception is ValidationException validationException)
        {
            code = HttpStatusCode.BadRequest;
            result = JsonSerializer.Serialize(new { 
                error = "Validation Failed", 
                details = validationException.Errors.Select(e => e.ErrorMessage) 
            });
        }
        else if (exception is Stripe.StripeException stripeEx)
        {
            code = HttpStatusCode.BadRequest;
            result = JsonSerializer.Serialize(new { error = stripeEx.Message });
        }
        else if (exception is Npgsql.PostgresException pgEx && pgEx.SqlState == "P0001")
        {
            code = HttpStatusCode.Conflict;
            result = JsonSerializer.Serialize(new { error = "Pricing was updated by another user. Please refresh." });
        }
        else if (exception is ArgumentException || exception is InvalidOperationException)
        {
            code = HttpStatusCode.BadRequest;
            result = JsonSerializer.Serialize(new { error = exception.Message });
        }
        else
        {
            result = JsonSerializer.Serialize(new { error = "An internal server error occurred." });
        }

        context.Response.ContentType = "application/json";
        context.Response.StatusCode = (int)code;

        return context.Response.WriteAsync(result);
    }
}
