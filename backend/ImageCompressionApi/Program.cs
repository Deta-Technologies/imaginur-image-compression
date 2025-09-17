using ImageCompressionApi.Models;
using ImageCompressionApi.Services;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.OpenApi.Models;
using System.Reflection;
using ImageCompressionApi.BackgroundServices;
using ImageCompressionApi.Configurations;
using ImageCompressionApi.Middleware;

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

await app.ConfigureFfmpeg();

app.Run();







