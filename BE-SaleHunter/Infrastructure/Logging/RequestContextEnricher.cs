using Serilog.Core;
using Serilog.Events;

namespace BE_SaleHunter.Infrastructure.Logging
{
    public class RequestContextEnricher : ILogEventEnricher
    {
        public void Enrich(LogEvent logEvent, ILogEventPropertyFactory propertyFactory)
        {
            var httpContext = GetHttpContext();
            if (httpContext != null)
            {
                // Add request ID if available
                if (httpContext.Items.TryGetValue("RequestId", out var requestId))
                {
                    logEvent.AddPropertyIfAbsent(propertyFactory.CreateProperty("RequestId", requestId));
                }

                // Add user ID if available
                var userId = httpContext.User?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                if (!string.IsNullOrEmpty(userId))
                {
                    logEvent.AddPropertyIfAbsent(propertyFactory.CreateProperty("UserId", userId));
                }

                // Add endpoint info
                var endpoint = httpContext.GetEndpoint();
                if (endpoint != null)
                {
                    logEvent.AddPropertyIfAbsent(propertyFactory.CreateProperty("Endpoint", endpoint.DisplayName));
                }
            }
        }

        private static HttpContext? GetHttpContext()
        {
            try
            {
                var httpContextAccessor = ServiceLocator.GetService<IHttpContextAccessor>();
                return httpContextAccessor?.HttpContext;
            }
            catch
            {
                return null;
            }
        }
    }

    // Simple service locator for accessing DI container
    public static class ServiceLocator
    {
        private static IServiceProvider? _serviceProvider;

        public static void SetServiceProvider(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        public static T? GetService<T>()
        {
            return _serviceProvider != null ? _serviceProvider.GetService<T>() : default;
        }
    }
}