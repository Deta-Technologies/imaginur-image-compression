# Build stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY . .
RUN dotnet restore
RUN dotnet publish -c Release -o /app/publish

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:8.0

# Install FFmpeg and curl (curl for healthchecks)
RUN apt-get update && \
    apt-get install -y ffmpeg curl && \
    rm -rf /var/lib/apt/lists/*

WORKDIR /app
COPY --from=build /app/publish .

# Expose ports (can be overridden by docker-compose)
EXPOSE 5000 5001 10000

# Create temp directory for file uploads
RUN mkdir -p /app/wwwroot/temp

ENTRYPOINT ["dotnet", "ImageCompressionApi.dll"]