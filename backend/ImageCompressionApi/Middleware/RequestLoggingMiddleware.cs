namespace ImageCompressionApi.Middleware
{
    /// <summary>
    /// Middleware for request logging
    /// </summary>
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
            var startTime = DateTime.UtcNow;
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();

            try
            {
                await _next(context);
            }
            finally
            {
                stopwatch.Stop();
                var responseTime = stopwatch.ElapsedMilliseconds;
            
                _logger.LogInformation(
                    "HTTP {Method} {Path} responded {StatusCode} in {ResponseTime}ms",
                    context.Request.Method,
                    context.Request.Path,
                    context.Response.StatusCode,
                    responseTime);
            }
        }
    }
}