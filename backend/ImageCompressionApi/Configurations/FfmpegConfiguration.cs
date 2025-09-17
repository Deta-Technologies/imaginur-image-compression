using ImageCompressionApi.Services;

namespace ImageCompressionApi.Configurations;

public static class FfmpegConfiguration
{
    public static async Task ConfigureFfmpeg(this WebApplication app)
    {
        
        var logger = app.Services.GetRequiredService<ILogger<Program>>();
        logger.LogInformation("Image Compression API starting up...");

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
    }
}