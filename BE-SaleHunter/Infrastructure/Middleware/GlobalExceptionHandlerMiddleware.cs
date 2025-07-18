using System.Net;
using System.Text.Json;
using BE_SaleHunter.Application.DTOs;

namespace BE_SaleHunter.Infrastructure.Middleware
{
    public class GlobalExceptionHandlerMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<GlobalExceptionHandlerMiddleware> _logger;

        public GlobalExceptionHandlerMiddleware(RequestDelegate next, ILogger<GlobalExceptionHandlerMiddleware> logger)
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
                _logger.LogError(ex, "An unhandled exception occurred");
                await HandleExceptionAsync(context, ex);
            }
        }        private static async Task HandleExceptionAsync(HttpContext context, Exception exception)
        {
            context.Response.ContentType = "application/json";

            var response = new BaseResponseDto<object>();

            switch (exception)
            {
                case ArgumentException:
                    response.Code = (int)HttpStatusCode.BadRequest;
                    response.Message = exception.Message;
                    context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                    break;

                case UnauthorizedAccessException:
                    response.Code = (int)HttpStatusCode.Unauthorized;
                    response.Message = "Unauthorized access";
                    context.Response.StatusCode = (int)HttpStatusCode.Unauthorized;
                    break;

                case KeyNotFoundException:
                    response.Code = (int)HttpStatusCode.NotFound;
                    response.Message = "Resource not found";
                    context.Response.StatusCode = (int)HttpStatusCode.NotFound;
                    break;

                case InvalidOperationException:
                    response.Code = (int)HttpStatusCode.BadRequest;
                    response.Message = "Invalid operation";
                    context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                    break;

                default:
                    response.Code = (int)HttpStatusCode.InternalServerError;
                    response.Message = "An error occurred while processing your request";
                    context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
                    break;
            }

            var jsonResponse = JsonSerializer.Serialize(response, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });

            await context.Response.WriteAsync(jsonResponse);
        }
    }

    // Extension method to register the middleware
    public static class GlobalExceptionHandlerMiddlewareExtensions
    {
        public static IApplicationBuilder UseGlobalExceptionHandler(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<GlobalExceptionHandlerMiddleware>();
        }
    }
}
