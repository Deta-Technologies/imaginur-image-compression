using ImageCompressionApi.Models;
using ImageCompressionApi.Services;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.OpenApi.Models;
using System.Reflection;
using Swashbuckle.AspNetCore.SwaggerGen;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddControllers();

// Configure settings
builder.Services.Configure<AppSettings>(builder.Configuration);

// Add image compression services
builder.Services.AddSingleton<IImageCompressionService, ImageCompressionService>();
builder.Services.AddSingleton<FileValidationService>();

builder.Services.AddCors(options =>
   {
       options.AddPolicy("AllowFrontend",
           policy => policy
               .WithOrigins(
                   "https://imaginur-image-compression.vercel.app", // your Vercel frontend
                   "http://localhost:8081" // (optional) for local dev
               )
               .AllowAnyHeader()
               .AllowAnyMethod());
   });

// Configure CORS
var corsSettings = builder.Configuration.GetSection("Cors");
builder.Services.AddCors(options =>
{
    options.AddPolicy("ImageCompressionPolicy", policy =>
    {
        var origins = corsSettings.GetSection("AllowedOrigins").Get<string[]>() ?? new[] { "*" };
        var methods = corsSettings.GetSection("AllowedMethods").Get<string[]>() ?? new[] { "GET", "POST" };
        var headers = corsSettings.GetSection("AllowedHeaders").Get<string[]>() ?? new[] { "*" };
        var allowCredentials = corsSettings.GetValue<bool>("AllowCredentials", false);

        if (origins.Contains("*"))
        {
            policy.AllowAnyOrigin();
        }
        else
        {
            policy.WithOrigins(origins);
        }

        if (methods.Contains("*"))
        {
            policy.AllowAnyMethod();
        }
        else
        {
            policy.WithMethods(methods);
        }

        if (headers.Contains("*"))
        {
            policy.AllowAnyHeader();
        }
        else
        {
            policy.WithHeaders(headers);
        }

        if (allowCredentials && !origins.Contains("*"))
        {
            policy.AllowCredentials();
        }
    });
});

// Configure file upload limits
builder.Services.Configure<FormOptions>(options =>
{
    var maxFileSize = builder.Configuration.GetValue<long>("ImageCompression:MaxFileSizeBytes", 10485760);
    options.MultipartBodyLengthLimit = maxFileSize;
    options.ValueLengthLimit = int.MaxValue;
    options.MultipartHeadersLengthLimit = int.MaxValue;
});

// Configure Kestrel server options
builder.WebHost.ConfigureKestrel(serverOptions =>
{
    var maxFileSize = builder.Configuration.GetValue<long>("ImageCompression:MaxFileSizeBytes", 10485760);
    serverOptions.Limits.MaxRequestBodySize = maxFileSize;
});

// Add Swagger/OpenAPI
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Image Compression API",
        Version = "v1",
        Description = "An API for compressing images using FFmpeg",
        Contact = new OpenApiContact
        {
            Name = "Image Compression API",
            Email = "support@imagecompression.com"
        }
    });

    // Add XML documentation
    var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    if (File.Exists(xmlPath))
    {
        c.IncludeXmlComments(xmlPath);
    }

    // Add file upload support in Swagger
    c.OperationFilter<FileUploadOperationFilter>();
});

// Add hosted service for background cleanup
builder.Services.AddHostedService<CleanupBackgroundService>();

// Add logging
builder.Services.AddLogging(logging =>
{
    logging.AddConsole();
    logging.AddDebug();
});

var app = builder.Build();

app.UseCors("AllowFrontend");

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Image Compression API V1");
        c.RoutePrefix = string.Empty; // Serve Swagger UI at root
    });
}

// Enable CORS
app.UseCors("ImageCompressionPolicy");

// Serve static files from wwwroot
app.UseStaticFiles();

// Add custom middleware for request logging
app.UseMiddleware<RequestLoggingMiddleware>();

// Add error handling middleware
app.UseMiddleware<ErrorHandlingMiddleware>();

app.UseRouting();

app.UseAuthorization();

app.MapControllers();

// Add health check endpoint
app.MapGet("/health", async (IImageCompressionService compressionService) =>
{
    var isHealthy = await compressionService.IsFFmpegAvailableAsync();
    var version = await compressionService.GetFFmpegVersionAsync();
    
    return Results.Ok(new
    {
        Status = isHealthy ? "Healthy" : "Unhealthy",
        FFmpegVersion = version,
        Timestamp = DateTime.UtcNow
    });
});

// Log startup information
var logger = app.Services.GetRequiredService<ILogger<Program>>();
logger.LogInformation("Image Compression API starting up...");

// Verify FFmpeg availability at startup
var compressionService = app.Services.GetRequiredService<IImageCompressionService>();
var ffmpegAvailable = await compressionService.IsFFmpegAvailableAsync();
if (ffmpegAvailable)
{
    var ffmpegVersion = await compressionService.GetFFmpegVersionAsync();
    logger.LogInformation("FFmpeg is available. Version: {Version}", ffmpegVersion);
}
else
{
    logger.LogWarning("FFmpeg is not available. Image compression will not work until FFmpeg is installed and configured.");
}

app.Run();

/// <summary>
/// Swagger operation filter for file upload
/// </summary>
public class FileUploadOperationFilter : IOperationFilter
{
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        if (operation.RequestBody?.Content?.ContainsKey("multipart/form-data") == true)
        {
            var formDataContent = operation.RequestBody.Content["multipart/form-data"];
            if (formDataContent.Schema?.Properties?.ContainsKey("file") == true)
            {
                formDataContent.Schema.Properties["file"] = new OpenApiSchema
                {
                    Type = "string",
                    Format = "binary",
                    Description = "The image file to compress"
                };
            }
        }
    }
}

/// <summary>
/// Background service for cleaning up expired files
/// </summary>
public class CleanupBackgroundService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<CleanupBackgroundService> _logger;
    private readonly TimeSpan _cleanupInterval = TimeSpan.FromMinutes(10);

    public CleanupBackgroundService(
        IServiceProvider serviceProvider,
        ILogger<CleanupBackgroundService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Cleanup background service started");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var compressionService = scope.ServiceProvider.GetRequiredService<IImageCompressionService>();
                
                var deletedCount = await compressionService.CleanupExpiredFilesAsync();
                if (deletedCount > 0)
                {
                    _logger.LogInformation("Background cleanup completed. Deleted {Count} files", deletedCount);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during background cleanup");
            }

            await Task.Delay(_cleanupInterval, stoppingToken);
        }

        _logger.LogInformation("Cleanup background service stopped");
    }
}

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