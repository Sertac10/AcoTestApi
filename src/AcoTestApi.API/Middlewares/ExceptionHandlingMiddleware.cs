using System;
using System.Linq;
using System.Net;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using AcoTestApi.Application.Common.Exceptions;

namespace AcoTestApi.API.Middlewares;

public class ExceptionHandlingMiddleware : IMiddleware
{
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;
    private readonly IWebHostEnvironment _env;

    public ExceptionHandlingMiddleware(ILogger<ExceptionHandlingMiddleware> logger, IWebHostEnvironment env)
    {
        _logger = logger;
        _env = env;
    }

    public async Task InvokeAsync(HttpContext context, RequestDelegate next)
    {
        try
        {
            await next(context);
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(context, ex);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception ex)
    {
        var options = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
        context.Response.ContentType = "application/json";

        if (ex is CustomValidationException valEx)
        {
            var responseMessage = valEx.GetErrorMessage();
            var validationErrors = valEx.GetValidationErrors();
            
            var details = validationErrors.Count > 0
                ? validationErrors.Select(x => new { field = x.Field, message = x.Message }).ToList()
                : responseMessage.Split(new[] { ", " }, StringSplitOptions.RemoveEmptyEntries)
                    .Select(x => new { field = string.Empty, message = x }).ToList();

            var errorResponse = new
            {
                error = details.FirstOrDefault()?.message ?? responseMessage,
                type = "ValidationError",
                timestamp = DateTime.UtcNow,
                details
            };

            context.Response.StatusCode = (int)valEx.GetStatusCode();
            var json = JsonSerializer.Serialize(errorResponse, options);
            await context.Response.WriteAsync(json);
            _logger.LogWarning(ex, "Validation error occurred.");
        }
        else if (ex is AppException appEx)
        {
            var errorResponse = new
            {
                error = appEx.GetErrorMessage(),
                type = "AppError",
                timestamp = DateTime.UtcNow
            };

            context.Response.StatusCode = (int)appEx.GetStatusCode();
            var json = JsonSerializer.Serialize(errorResponse, options);
            await context.Response.WriteAsync(json);
            _logger.LogWarning(ex, "App error occurred: {Message}", appEx.GetErrorMessage());
        }
        else
        {
            object errorResponse;
            if (_env.IsDevelopment())
            {
                errorResponse = new
                {
                    error = ex.Message,
                    type = "InternalServerError",
                    timestamp = DateTime.UtcNow,
                    traceId = context.TraceIdentifier,
                    stackTrace = ex.StackTrace,
                    innerException = ex.InnerException?.Message
                };
            }
            else
            {
                errorResponse = new
                {
                    error = "Beklenmedik bir sunucu hatası oluştu. Lütfen tekrar deneyiniz.",
                    type = "InternalServerError",
                    timestamp = DateTime.UtcNow,
                    traceId = context.TraceIdentifier
                };
            }

            context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
            var json = JsonSerializer.Serialize(errorResponse, options);
            await context.Response.WriteAsync(json);
            _logger.LogError(ex, "An unhandled exception occurred.");
        }
    }
}
