# Image Compression Web App Assignment Specification

## Overview
Build a simple image compression application consisting of a frontend web client and an ASP.NET Core backend. The application should allow users to drag and drop images for compression using FFmpeg and return the optimized image for download.

## Part 1: Frontend Application

### Requirements
- **Technology**: Vanilla JavaScript, HTML5, CSS3 (no frameworks required)
- **Core Functionality**: 
  - Drag and drop area for image upload
  - Support for common image formats (JPEG, PNG, WebP, BMP)
  - Display original image preview
  - Show compression progress indicator
  - Display compressed image preview with file size comparison
  - Download button for compressed image
  - Handle loading states and error messages

### Technical Specifications
- Use HTML5 File API for drag and drop functionality
- Implement FormData for multipart file uploads
- Use modern JavaScript (ES6+) features
- Display file size before and after compression
- Show compression ratio/percentage saved
- Implement proper error handling for unsupported files
- Maximum file size validation (e.g., 10MB limit)

### UI/UX Requirements
- Clear drag and drop zone with visual feedback
- Loading spinner during compression process
- Side-by-side comparison of original vs compressed images
- File size and compression statistics display
- Clean, modern interface design
- Responsive design for mobile and desktop
- Visual indicators for supported file types

### File Upload Features
- Drag and drop functionality
- Click to browse alternative
- File type validation (images only)
- File size validation
- Multiple file format support
- Preview generation before upload

### Deliverables
- `index.html` - Main HTML structure with drag/drop area
- `style.css` - Styling and responsive design
- `script.js` - JavaScript functionality for file handling and API calls
- `README.md` - Setup and usage instructions

## Part 2: ASP.NET Core Backend

### Requirements
- **Technology**: ASP.NET Core 8.0 or later
- **Core Functionality**: Accept image uploads, compress using FFmpeg, return compressed images
- **FFmpeg Integration**: Use FFmpeg for image compression and format conversion
- **File Handling**: Multipart form data processing

### Technical Specifications
- RESTful API endpoint: `POST /api/images/compress`
- Accept multipart/form-data file uploads
- Integrate FFmpeg for image processing
- Support multiple image formats (JPEG, PNG, WebP, BMP)
- Implement file validation and security checks
- Temporary file management and cleanup
- Configurable compression settings
- Proper error handling and logging

### API Endpoint Specification

#### Compress Image Endpoint
**URL**: `POST /api/images/compress`
**Content-Type**: `multipart/form-data`

**Request Parameters**:
- `file` (required) - The image file to compress
- `quality` (optional) - Compression quality (1-100, default: 80)
- `format` (optional) - Output format (jpeg, png, webp, default: same as input)

**Success Response** (200 OK):
```json
{
  "success": true,
  "data": {
    "originalSize": 2048576,
    "compressedSize": 512144,
    "compressionRatio": 75.0,
    "format": "jpeg",
    "downloadUrl": "/api/images/download/temp-file-id"
  }
}
```

**Error Response** (400 Bad Request):
```json
{
  "success": false,
  "error": {
    "message": "Unsupported file format",
    "code": "INVALID_FILE_FORMAT"
  }
}
```

### FFmpeg Integration Requirements
- Install FFmpeg on the server
- Use System.Diagnostics.Process to execute FFmpeg commands
- Implement proper command-line argument sanitization
- Handle FFmpeg execution errors gracefully
- Support various compression parameters:
  - JPEG: Quality setting, progressive encoding
  - PNG: Compression level, optimization
  - WebP: Quality and lossless options

### Example FFmpeg Commands
```bash
# JPEG compression
ffmpeg -i input.jpg -q:v 5 -optimize output.jpg

# PNG compression
ffmpeg -i input.png -compression_level 9 output.png

# WebP conversion
ffmpeg -i input.jpg -c:v libwebp -q:v 80 output.webp
```

### Security & File Handling
- File type validation using magic numbers/file headers
- File size limits (configurable, e.g., 10MB max)
- Temporary file cleanup after processing
- Input sanitization for FFmpeg commands
- Rate limiting to prevent abuse
- Virus scanning (optional but recommended)

### Configuration Settings
```json
{
  "ImageCompression": {
    "MaxFileSizeBytes": 10485760,
    "AllowedFormats": ["jpeg", "jpg", "png", "webp", "bmp"],
    "DefaultQuality": 80,
    "TempFileRetentionMinutes": 30,
    "FFmpegPath": "/usr/bin/ffmpeg"
  }
}
```

### Required NuGet Packages
- Microsoft.AspNetCore.Http
- Microsoft.Extensions.Logging
- System.Drawing.Common (for image metadata)
- Microsoft.AspNetCore.StaticFiles (for file serving)

### Deliverables
- `Program.cs` - Application entry point and configuration
- `Controllers/ImageController.cs` - API controller for image processing
- `Services/ImageCompressionService.cs` - FFmpeg integration service
- `Models/ImageCompressionRequest.cs` - Request models
- `Models/ImageCompressionResponse.cs` - Response models
- `appsettings.json` - Configuration settings
- `README.md` - Setup, installation, and API documentation

## General Requirements

### Documentation
- Clear README files for both frontend and backend
- Include setup instructions and prerequisites
- Document API endpoints and expected responses
- FFmpeg installation and configuration guide
- Provide example usage and screenshots

### Code Quality
- Use consistent coding style and formatting
- Implement proper exception handling
- Follow C# and JavaScript best practices
- Include XML documentation for API methods
- Use dependency injection in ASP.NET Core
- Implement proper logging throughout

### Testing
- Manual testing of all functionality
- Test various image formats and sizes
- Test error scenarios (corrupted files, unsupported formats)
- Verify file cleanup and memory management
- Test compression quality settings
- Performance testing with large files

## Installation & Setup Requirements

### Backend Prerequisites
- .NET 8.0 SDK or later
- FFmpeg installed on the system
- Visual Studio or VS Code (recommended)

### FFmpeg Installation
- **Windows**: Download from ffmpeg.org or use chocolatey
- **Linux**: `sudo apt-get install ffmpeg` (Ubuntu/Debian)
- **macOS**: `brew install ffmpeg`

### Frontend Prerequisites
- Modern web browser with HTML5 support
- Local web server for testing (optional)

## Bonus Features (Optional)
- Batch image processing (multiple files at once)
- Custom compression presets (web, print, archive)
- Image format conversion options
- Resize functionality alongside compression
- Progress tracking for large file processing
- Image metadata preservation options
- Custom watermarking
- Advanced FFmpeg parameters exposure

## Technical Considerations
- **Performance**: Handle large files efficiently without blocking
- **Memory Management**: Proper disposal of file streams and temporary files
- **Scalability**: Consider async processing for production use
- **Security**: Validate all inputs and sanitize FFmpeg commands
- **Error Handling**: Graceful handling of FFmpeg failures
- **Logging**: Comprehensive logging for debugging and monitoring

## Submission Guidelines
1. Create separate folders for frontend and backend code
2. Include all source files and configuration
3. Provide clear installation and setup instructions
4. Include screenshots of the working application
5. Document FFmpeg installation process
6. Test thoroughly with various image types and sizes

## Evaluation Criteria
- **Functionality**: Does the application compress images correctly?
- **Code Quality**: Is the code clean, readable, and well-structured?
- **User Experience**: Is the drag-and-drop interface intuitive?
- **Error Handling**: Are errors handled gracefully?
- **Documentation**: Are setup instructions clear and complete?
- **Security**: Are file uploads handled securely?
- **Performance**: Does the app handle large files efficiently?

## Timeline
- **Part 1 (Frontend)**: 2-3 days
- **Part 2 (Backend + FFmpeg)**: 4-5 days
- **Integration & Testing**: 2-3 days
- **Total Estimated Time**: 8-11 days

## Resources
- [ASP.NET Core Documentation](https://docs.microsoft.com/en-us/aspnet/core/)
- [FFmpeg Documentation](https://ffmpeg.org/documentation.html)
- [HTML5 File API](https://developer.mozilla.org/en-US/docs/Web/API/File)
- [Drag and Drop API](https://developer.mozilla.org/en-US/docs/Web/API/HTML_Drag_and_Drop_API)
- [System.Diagnostics.Process](https://docs.microsoft.com/en-us/dotnet/api/system.diagnostics.process)

## Important Notes
- Ensure FFmpeg is properly installed and accessible
- Implement proper error handling for FFmpeg execution failures
- Consider file size limits and server resources
- Test with various image formats and edge cases
- Implement proper temporary file cleanup to prevent disk space issues

Good luck building your image compression application!