using ImageCompressionApi.Models;
using ImageCompressionApi.Exceptions;

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

            var (statusCode, errorCode, message) = exception switch
            {
                FFmpegException ffmpegEx => (500, ErrorCodes.FFMPEG_ERROR, ffmpegEx.Message),
                ImageCompressionException compressionEx => (500, compressionEx.ErrorCode, compressionEx.Message),
                FileValidationException validationEx => (400, validationEx.ErrorCode, validationEx.Message),
                ArgumentException => (400, ErrorCodes.INVALID_PARAMETERS, exception.Message),
                FileNotFoundException => (404, ErrorCodes.FILE_NOT_FOUND, "File not found"),
                TimeoutException => (408, ErrorCodes.PROCESSING_TIMEOUT, "Operation timed out"),
                InvalidOperationException when exception.Message.Contains("FFmpeg") =>
                    (500, ErrorCodes.FFMPEG_ERROR, "Image processing failed"),
                _ => (500, ErrorCodes.UNKNOWN_ERROR, "An unexpected error occurred")
            };

            context.Response.StatusCode = statusCode;

            var response = ApiResponse<object>.CreateError(message, errorCode);
            await context.Response.WriteAsJsonAsync(response);
        }
    }
}
