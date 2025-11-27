using ImageCompressionApi.Models;
using ImageCompressionApi.Services;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.OpenApi.Models;
using System.Reflection;
using ImageCompressionApi.BackgroundServices;
using ImageCompressionApi.Configurations;
using ImageCompressionApi.Middleware;
using Serilog;
using Serilog.Events;

// Bootstrap Logger - provides early logging during application startup
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Override("Microsoft", LogEventLevel.Information)
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .CreateBootstrapLogger();

try
{
    Log.Information("Starting Image Compression API");

    var builder = WebApplication.CreateBuilder(args);

    // Configure Serilog with full configuration from appsettings.json
    builder.Host.UseSerilog((context, services, configuration) => configuration
        .ReadFrom.Configuration(context.Configuration)
        .ReadFrom.Services(services)
        .Enrich.FromLogContext()
        .WriteTo.Console(
            outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj} {Properties:j}{NewLine}{Exception}")
        .WriteTo.File(
            path: "logs/log-.txt",
            rollingInterval: RollingInterval.Day,
            rollOnFileSizeLimit: true,
            fileSizeLimitBytes: 20_971_520, // 20 MB
            retainedFileCountLimit: 31,
            outputTemplate: "[{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} {Level:u3}] {Message:lj} {Properties:j}{NewLine}{Exception}")
    );

    // Add services to the container
    builder.Services.AddControllers();

    // Configure settings
    builder.Services.Configure<AppSettings>(builder.Configuration);

    // Add file system abstraction
    builder.Services.AddSingleton<System.IO.Abstractions.IFileSystem, System.IO.Abstractions.FileSystem>();

    // Add core services
    builder.Services.AddSingleton<IFileStorageService, FileStorageService>();
    builder.Services.AddSingleton<IFFmpegExecutor, FFmpegExecutor>();
    builder.Services.AddSingleton<IImageFormatService, ImageFormatService>();

    // Add image compression services
    builder.Services.AddSingleton<IImageCompressionService, ImageCompressionService>();
    builder.Services.AddSingleton<FileValidationService>();

    // Configure CORS
    builder.Services.AddCors(options =>
    {
        options.AddPolicy("AllowFrontend",
            policy => policy
                .WithOrigins(
                    "https://imaginur-image-compression.vercel.app", // Vercel frontend
                    "http://localhost:8081",  // Local dev
                    "http://localhost:8080"   // Alternative local dev port
                )
                .AllowAnyHeader()
                .AllowAnyMethod());
    });

    // Read max file size configuration once
    var maxFileSize = builder.Configuration.GetValue<long>("ImageCompression:MaxFileSizeBytes", 10485760);

    // Configure file upload limits
    builder.Services.Configure<FormOptions>(options =>
    {
        options.MultipartBodyLengthLimit = maxFileSize;
        options.ValueLengthLimit = int.MaxValue;
        options.MultipartHeadersLengthLimit = int.MaxValue;
    });

    // Configure Kestrel server options
    builder.WebHost.ConfigureKestrel(serverOptions =>
    {
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

    var app = builder.Build();

    // Enable CORS
    app.UseCors("AllowFrontend");

    // Add Serilog request logging - provides HTTP request/response logging
    app.UseSerilogRequestLogging(options =>
    {
        options.MessageTemplate = "HTTP {RequestMethod} {RequestPath} responded {StatusCode} in {Elapsed:0.0000} ms";
        options.GetLevel = (httpContext, elapsed, ex) => ex != null
            ? LogEventLevel.Error
            : elapsed > 1000
                ? LogEventLevel.Warning
                : LogEventLevel.Information;

        options.EnrichDiagnosticContext = (diagnosticContext, httpContext) =>
        {
            diagnosticContext.Set("RequestHost", httpContext.Request.Host.Value);
            diagnosticContext.Set("RequestScheme", httpContext.Request.Scheme);
            diagnosticContext.Set("UserAgent", httpContext.Request.Headers["User-Agent"].ToString());
            diagnosticContext.Set("ClientIP", httpContext.Connection.RemoteIpAddress?.ToString());
        };
    });

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

    // Serve static files from wwwroot
    app.UseStaticFiles();

    // Custom RequestLoggingMiddleware removed - using Serilog's UseSerilogRequestLogging instead

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

    await app.ConfigureFfmpeg();

    Log.Information("Image Compression API started successfully");
    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application terminated unexpectedly");
    throw;
}
finally
{
    Log.CloseAndFlush();
}







