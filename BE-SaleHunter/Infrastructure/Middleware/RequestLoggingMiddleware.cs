using System.Diagnostics;
using System.Text;
using System.Text.Json;

namespace BE_SaleHunter.Infrastructure.Middleware
{
    public class RequestLoggingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<RequestLoggingMiddleware> _logger;

        public RequestLoggingMiddleware(RequestDelegate next, ILogger<RequestLoggingMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            var stopwatch = Stopwatch.StartNew();
            var requestId = Guid.NewGuid().ToString("N")[..8];

            // Add request ID to context for tracking
            context.Items["RequestId"] = requestId;            try
            {
                // Log incoming request
                await LogRequestAsync(context, requestId);

                // Add request context to Serilog context
                using (Serilog.Context.LogContext.PushProperty("RequestId", requestId))
                using (Serilog.Context.LogContext.PushProperty("RequestPath", context.Request.Path))
                using (Serilog.Context.LogContext.PushProperty("ConnectionId", context.Connection.Id))
                {
                    // Capture response for logging
                    var originalResponseBodyStream = context.Response.Body;
                    using var responseBody = new MemoryStream();
                    context.Response.Body = responseBody;

                    // Continue to next middleware
                    await _next(context);

                    // Log response
                    stopwatch.Stop();
                    await LogResponseAsync(context, requestId, stopwatch.ElapsedMilliseconds, responseBody);

                    // Copy response back to original stream
                    await responseBody.CopyToAsync(originalResponseBodyStream);
                }
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                _logger.LogError(ex,
                    "REQUEST FAILED - RequestId: {RequestId}, Method: {Method}, Path: {Path}, Duration: {Duration}ms",
                    requestId, context.Request.Method, context.Request.Path, stopwatch.ElapsedMilliseconds);
                throw;
            }
        }

        private async Task LogRequestAsync(HttpContext context, string requestId)
        {
            var request = context.Request;
            var requestBody = string.Empty; // Read request body for POST/PUT requests
            if (request is { ContentLength: > 0, Method: "POST" or "PUT" or "PATCH" })
            {
                request.EnableBuffering();
                var buffer = new byte[Convert.ToInt32(request.ContentLength)];
                var bytesRead = await request.Body.ReadAsync(buffer.AsMemory(0, buffer.Length));
                requestBody = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                request.Body.Position = 0;

                // Sanitize sensitive data
                requestBody = SanitizeRequestBody(requestBody);
            }

            // Extract user information
            var userId = context.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            var userEmail = context.User.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value;

            // Log request details
            _logger.LogInformation(
                "REQUEST RECEIVED - RequestId: {RequestId}, Method: {Method}, Path: {Path}, Query: {Query}, " +
                "ContentType: {ContentType}, ContentLength: {ContentLength}, UserAgent: {UserAgent}, " +
                "RemoteIP: {RemoteIP}, UserId: {UserId}, UserEmail: {UserEmail}, Body: {Body}",
                requestId,
                request.Method,
                request.Path,
                request.QueryString.ToString(),
                request.ContentType,
                request.ContentLength,
                request.Headers.UserAgent.ToString(),
                GetClientIpAddress(context),
                userId ?? "Anonymous",
                userEmail ?? "N/A",
                string.IsNullOrEmpty(requestBody) ? "N/A" : requestBody
            );
        }

        private async Task LogResponseAsync(HttpContext context, string requestId, long durationMs,
            MemoryStream responseBody)
        {
            var response = context.Response;
            var responseBodyText = string.Empty;

            // Read response body
            if (responseBody.Length > 0)
            {
                responseBody.Seek(0, SeekOrigin.Begin);
                responseBodyText = await new StreamReader(responseBody).ReadToEndAsync();
                responseBody.Seek(0, SeekOrigin.Begin);

                // Truncate long responses for logging
                if (responseBodyText.Length > 1000)
                {
                    responseBodyText = responseBodyText[..1000] + "... (truncated)";
                }
            }

            // Determine log level based on status code
            var logLevel = GetLogLevelForStatusCode(response.StatusCode);

            _logger.Log(logLevel,
                "REQUEST COMPLETED - RequestId: {RequestId}, StatusCode: {StatusCode}, Duration: {Duration}ms, " +
                "ContentType: {ContentType}, ContentLength: {ContentLength}, Response: {Response}",
                requestId,
                response.StatusCode,
                durationMs,
                response.ContentType,
                response.ContentLength,
                responseBodyText
            );

            // Log performance warning for slow requests
            if (durationMs > 5000) // 5 seconds
            {
                _logger.LogWarning(
                    "SLOW REQUEST DETECTED - RequestId: {RequestId}, Duration: {Duration}ms, Path: {Path}",
                    requestId, durationMs, context.Request.Path);
            }
        }

        private string SanitizeRequestBody(string requestBody)
        {
            if (string.IsNullOrEmpty(requestBody))
                return requestBody;

            try
            {
                // Parse JSON and sanitize sensitive fields
                var jsonDocument = JsonDocument.Parse(requestBody);
                var sanitizedJson = SanitizeJsonElement(jsonDocument.RootElement);
                return JsonSerializer.Serialize(sanitizedJson, new JsonSerializerOptions { WriteIndented = false });
            }
            catch
            {
                // If not valid JSON, return as-is but mask potential sensitive data
                return MaskSensitiveData(requestBody);
            }
        }

        private object SanitizeJsonElement(JsonElement element)
        {
            switch (element.ValueKind)
            {
                case JsonValueKind.Object:
                    var obj = new Dictionary<string, object>();
                    foreach (var property in element.EnumerateObject())
                    {
                        var key = property.Name.ToLower();
                        obj[property.Name] = IsSensitiveField(key) 
                            ? "***MASKED***" 
                            : SanitizeJsonElement(property.Value);
                    }

                    return obj;

                case JsonValueKind.Array:
                    return element.EnumerateArray().Select(SanitizeJsonElement).ToArray();
                case JsonValueKind.String:
                    return element.GetString() ?? string.Empty;

                case JsonValueKind.Number:
                    return element.GetDouble();

                case JsonValueKind.True:
                case JsonValueKind.False:
                    return element.GetBoolean();

                case JsonValueKind.Null:
                    return "null";

                case JsonValueKind.Undefined:
                default:
                    return element.ToString();
            }
        }

        private bool IsSensitiveField(string fieldName)
        {
            var sensitiveFields = new[]
            {
                "password", "pwd", "secret", "token", "key", "authorization",
                "creditcard", "ssn", "socialsecuritynumber", "pin", "cvv"
            };

            return sensitiveFields.Any(fieldName.Contains);
        }

        private string MaskSensitiveData(string data)
        {
            // Simple regex-based masking for common patterns
            var patterns = new[]
            {
                """
                "password":\s*"[^"]*"
                """,
                """
                "token":\s*"[^"]*"
                """,
                """
                "secret":\s*"[^"]*"
                """
            };

            foreach (var pattern in patterns)
            {
                data = System.Text.RegularExpressions.Regex.Replace(data, pattern,
                    match => match.Value.Substring(0, match.Value.IndexOf(':') + 1) + " \"***MASKED***\"");
            }

            return data;
        }

        private string GetClientIpAddress(HttpContext context)
        {
            var xForwardedFor = context.Request.Headers["X-Forwarded-For"].FirstOrDefault();
            if (!string.IsNullOrEmpty(xForwardedFor))
            {
                return xForwardedFor.Split(',')[0].Trim();
            }

            var xRealIp = context.Request.Headers["X-Real-IP"].FirstOrDefault();
            if (!string.IsNullOrEmpty(xRealIp))
            {
                return xRealIp;
            }

            return context.Connection.RemoteIpAddress?.ToString() ?? "Unknown";
        }

        private LogLevel GetLogLevelForStatusCode(int statusCode)
        {
            return statusCode switch
            {
                >= 500 => LogLevel.Error,
                >= 400 => LogLevel.Warning,
                _ => LogLevel.Information
            };
        }
    }
}