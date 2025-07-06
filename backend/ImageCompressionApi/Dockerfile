# Use the official .NET 8.0 ASP.NET runtime image
FROM mcr.microsoft.com/dotnet/aspnet:8.0

# Install FFmpeg
RUN apt-get update && apt-get install -y ffmpeg

# Set the working directory
WORKDIR /app

# Copy published output from build stage (if using multi-stage) or all files
COPY . ./

# Expose the port Render expects (default 10000)
EXPOSE 10000

# Start the application
ENTRYPOINT ["dotnet", "ImageCompressionApi.dll"] 