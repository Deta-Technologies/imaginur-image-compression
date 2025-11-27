# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

Full-stack image compression web application using FFmpeg for server-side compression. The frontend is a vanilla JavaScript SPA deployed on Vercel, and the backend is an ASP.NET Core 8.0 Web API deployed on Render.

**Tech Stack:**
- **Backend**: ASP.NET Core 8.0, FFmpeg, C# 12
- **Frontend**: Vanilla JavaScript (ES6+), HTML5, CSS3
- **Deployment**: Backend on Render (Docker), Frontend on Vercel

## Development Commands

### Backend (.NET 8.0)

Navigate to `backend/ImageCompressionApi` for all commands:

```bash
# Restore dependencies
dotnet restore

# Build the project
dotnet build

# Run the API (development mode with Swagger at http://localhost:5000)
dotnet run

# Run with specific configuration
dotnet run --configuration Debug
dotnet run --configuration Release

# Run tests (if test project exists)
dotnet test

# Build for production
dotnet publish -c Release -o ./publish

# Build Docker image
docker build -t image-compression-api .

# Run Docker container
docker run -p 5000:80 image-compression-api
```

### Frontend

Navigate to `frontend` directory:

```bash
# Option 1: Using Node.js http-server
http-server -p 8080 -c-1

# Option 2: Using Python
python -m http.server 8080

# Option 3: Open index.html directly in browser (for simple testing)
# Just open frontend/index.html in a web browser
```

### Testing

```bash
# Test backend health
curl http://localhost:5000/api/image/health

# Test image compression
curl -X POST -F "file=@test.jpg" -F "quality=80" http://localhost:5000/api/image/compress

# Manual FFmpeg test
ffmpeg -i input.jpg -q:v 5 output.jpg
```

## Architecture

### Backend Architecture

**Entry Point**: `Program.cs` - Configures all services, middleware, CORS, and dependency injection.

**Core Services** (in `Services/`):
- `ImageCompressionService`: Main compression logic using FFmpeg via Process API. Manages semaphore for concurrent operations (default: 5), handles file I/O in `wwwroot/temp/`, and constructs FFmpeg command-line arguments for different formats.
- `FileValidationService`: Validates uploaded files using magic number validation (reads first bytes) to prevent file type spoofing. Checks file size, format, and MIME type.

**Controllers** (in `Controllers/`):
- `ImageController`: REST API endpoints:
  - `POST /api/image/compress` - Upload and compress image
  - `GET /api/image/download/{fileId}` - Download compressed file
  - `GET /api/image/health` - FFmpeg availability check
  - `POST /api/image/cleanup` - Manual cleanup trigger
  - `GET /api/image/stats` - Configuration stats

**Middleware** (in `Middleware/`):
- `ErrorHandlingMiddleware`: Global exception handler, returns standardized API responses with error codes
- `RequestLoggingMiddleware`: Logs all incoming requests with timing information
- `FileUploadOperationFilter`: Swagger/OpenAPI filter for file upload documentation

**Background Services** (in `BackgroundServices/`):
- `CleanupBackgroundService`: IHostedService that runs every 5 minutes to delete files older than `TempFileRetentionMinutes` (default: 30) from `wwwroot/temp/`

**Configuration** (in `Configurations/`):
- `FfmpegConfiguration`: Startup extension method that validates FFmpeg availability and logs version info

**Models** (in `Models/`):
- `AppSettings`: Configuration binding model for appsettings.json
- `ImageCompressionRequest/Response`: DTOs for API
- `ApiResponse<T>`: Standardized API response wrapper with success/error structure

**Key Design Patterns**:
- Dependency Injection for all services
- Repository-like pattern for file operations
- Async/await throughout for non-blocking I/O
- Semaphore for concurrency control (prevents resource exhaustion)
- Process isolation for FFmpeg operations with timeout protection

### Frontend Architecture

**Files**:
- `index.html`: Single-page app structure with drag-drop zone, quality slider, format selector
- `css/style.css`: Responsive design with mobile-first approach
- `js/script.js`: Main application logic, handles file upload, API communication, progress tracking, and image preview

**Key Frontend Flow**:
1. User drags/drops or selects image file
2. Client-side validation (file type, size limit)
3. Form data constructed with file, quality, and format
4. Async POST to `/api/image/compress` with progress tracking
5. Display original vs compressed side-by-side with statistics
6. Download button triggers GET to `/api/image/download/{fileId}`

### FFmpeg Integration

The application uses FFmpeg as an external process, not a library. Key points:

- **Process execution**: `ImageCompressionService` spawns FFmpeg process with `ProcessStartInfo`
- **Command construction**: Different arguments per format (JPEG uses `-q:v`, PNG uses `-compression_level`, WebP uses `-c:v libwebp`)
- **Timeout handling**: Operations timeout after `FFmpegTimeoutSeconds` (default: 120s)
- **Path resolution**: Uses `ffmpeg` from system PATH or custom path from `appsettings.json`
- **Output capture**: Redirects stdout/stderr for error handling and logging

### File Lifecycle

1. **Upload**: File saved to `wwwroot/temp/{fileId}_original.{ext}`
2. **Compression**: FFmpeg reads original, writes to `wwwroot/temp/{fileId}_compressed.{ext}`
3. **Cleanup**: Original deleted immediately after compression, compressed file retained for `TempFileRetentionMinutes`
4. **Download**: Streams file directly from disk, doesn't load into memory
5. **Background cleanup**: `CleanupBackgroundService` removes expired files every 5 minutes

## Configuration

All settings in `appsettings.json`:

```json
{
  "ImageCompression": {
    "MaxFileSizeBytes": 10485760,          // 10MB limit
    "AllowedFormats": ["jpeg", "jpg", "png", "webp", "bmp"],
    "DefaultQuality": 80,
    "TempFileRetentionMinutes": 30,
    "FFmpegPath": "",                      // Leave empty to use system PATH
    "MaxConcurrentOperations": 5,
    "FFmpegTimeoutSeconds": 120
  },
  "Cors": {
    "AllowedOrigins": ["http://localhost:3000", "http://localhost:8080"],
    "AllowedMethods": ["GET", "POST", "PUT", "DELETE", "OPTIONS"],
    "AllowedHeaders": ["Content-Type", "Authorization"],
    "AllowCredentials": true
  }
}
```

**Important**: `Program.cs` has hardcoded CORS for Vercel deployment at lines 22-32. Update `AllowedOrigins` in both locations when changing frontend URL.

## Deployment

### Backend (Render)

Uses multi-stage Dockerfile:
1. Build stage: SDK 8.0 image, restore, and publish
2. Runtime stage: ASP.NET 8.0 image, install FFmpeg via apt-get, copy published files
3. Exposes port 10000 (Render requirement)

**Important**: The Dockerfile installs FFmpeg in the runtime container. Do not remove this step.

### Frontend (Vercel)

Static site deployment from `frontend/` directory. Update API URL in `js/script.js` to point to backend:

```javascript
// Update this line with backend URL
this.apiBaseUrl = 'https://your-backend.onrender.com/api';
```

## CORS Configuration

The application has **two CORS policies** configured in `Program.cs`:

1. **"AllowFrontend"** (lines 22-32): Hardcoded for Vercel and localhost
2. **"ImageCompressionPolicy"** (lines 36-77): Dynamic from appsettings.json

Currently uses `app.UseCors("AllowFrontend")` at line 135. If you need to modify CORS:
- For production: Update the hardcoded `AllowFrontend` origins
- For development: Update `appsettings.json` Cors section

## Common Issues

### FFmpeg Not Found
- **Symptom**: API returns "FFmpeg is not available" in health check
- **Fix**: Install FFmpeg (`choco install ffmpeg` on Windows, `brew install ffmpeg` on macOS, or specify path in `appsettings.json`)
- **Verify**: Run `ffmpeg -version` in terminal

### Compression Timeout
- **Symptom**: Operation times out on large files
- **Fix**: Increase `FFmpegTimeoutSeconds` in `appsettings.json`

### File Upload Fails
- **Symptom**: 413 Payload Too Large error
- **Fix**: All three limits must be increased together:
  1. `appsettings.json` → `ImageCompression:MaxFileSizeBytes`
  2. `appsettings.json` → `Kestrel:Limits:MaxRequestBodySize`
  3. `Program.cs` → `FormOptions.MultipartBodyLengthLimit` (line 83)

### CORS Errors
- **Symptom**: Browser blocks requests with CORS policy error
- **Fix**: Ensure frontend origin is in `AllowFrontend` policy in `Program.cs` (line 26-28)

## Code Style Notes

- Uses C# 12 features: file-scoped namespaces, top-level statements, global using directives
- Nullable reference types enabled (`<Nullable>enable</Nullable>`)
- Async/await pattern used consistently
- Structured logging with log levels (Debug, Information, Warning, Error)
- API responses use standardized `ApiResponse<T>` wrapper for consistency
- XML documentation comments on all public controllers and methods (for Swagger)

## Working With This Codebase

**When adding new image formats**:
1. Add format to `AllowedFormats` in `appsettings.json`
2. Update `BuildFFmpegArguments` in `ImageCompressionService.cs` (line 329)
3. Update `GetExtensionForFormat` and `GetContentTypeForExtension` methods
4. Add format validation in `FileValidationService.cs`

**When modifying FFmpeg commands**:
- Test manually first: `ffmpeg -i input.jpg [your args] output.jpg`
- Check FFmpeg documentation: https://ffmpeg.org/ffmpeg.html
- Remember to quote file paths in `BuildFFmpegArguments`
- Capture both stdout and stderr for debugging

**When changing file handling**:
- Ensure proper disposal of file streams (use `using` statements)
- Remember background cleanup service will delete old files
- Test with large files to avoid memory issues
- Consider disk space limits on deployment platform
