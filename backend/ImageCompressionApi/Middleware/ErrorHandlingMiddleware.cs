namespace ImageCompressionApi.Middleware
{
    /// <summary>
    /// Global error handling middleware
    /// </summary>
    public class ErrorHandlingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<ErrorHandlingMiddleware> _logger;

        public ErrorHandlingMiddleware(RequestDelegate next, ILogger<ErrorHandlingMiddleware> logger)
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
                _logger.LogError(ex, "Unhandled exception occurred");
                await HandleExceptionAsync(context, ex);
            }
        }

        private static async Task HandleExceptionAsync(HttpContext context, Exception exception)
        {
            context.Response.ContentType = "application/json";
        
            var response = new
            {
                success = false,
                error = new
                {
                    message = "An unexpected error occurred",
                    code = "INTERNAL_SERVER_ERROR"
                }
            };

            switch (exception)
            {
                case ArgumentException:
                    context.Response.StatusCode = 400;
                    response = new
                    {
                        success = false,
                        error = new
                        {
                            message = exception.Message,
                            code = "INVALID_PARAMETERS"
                        }
                    };
                    break;
                case FileNotFoundException:
                    context.Response.StatusCode = 404;
                    response = new
                    {
                        success = false,
                        error = new
                        {
                            message = "File not found",
                            code = "FILE_NOT_FOUND"
                        }
                    };
                    break;
                case TimeoutException:
                    context.Response.StatusCode = 408;
                    response = new
                    {
                        success = false,
                        error = new
                        {
                            message = "Operation timed out",
                            code = "PROCESSING_TIMEOUT"
                        }
                    };
                    break;
                default:
                    context.Response.StatusCode = 500;
                    break;
            }

            await context.Response.WriteAsync(System.Text.Json.JsonSerializer.Serialize(response));
        }
    }
} 