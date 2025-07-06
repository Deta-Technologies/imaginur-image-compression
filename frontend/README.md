# Image Compression Frontend

A modern, responsive web application for compressing images using drag-and-drop interface with real-time progress tracking and side-by-side comparison.

## Features

- **Drag & Drop Interface**: Intuitive file upload with visual feedback
- **Multiple Format Support**: JPEG, PNG, WebP, and BMP formats
- **Real-time Progress**: Live progress tracking during compression
- **Side-by-side Comparison**: Original vs compressed image preview
- **Compression Statistics**: File size reduction and compression ratio
- **Quality Control**: Adjustable compression quality (1-100)
- **Format Conversion**: Convert between different image formats
- **Responsive Design**: Works on desktop and mobile devices
- **Error Handling**: Comprehensive error messages and validation
- **File Size Validation**: 10MB maximum file size limit

## Prerequisites

- Modern web browser with HTML5 support
- Local web server (optional, for testing)
- Backend API server running (see backend README)

## Setup Instructions

### Option 1: Simple File Access
1. Extract the frontend files to a directory
2. Open `index.html` in a modern web browser
3. Ensure the backend API is running at `http://localhost:5000`

### Option 2: Local Web Server (Recommended)
1. Install a local web server:
   ```bash
   # Using Node.js http-server
   npm install -g http-server
   
   # Or using Python
   python -m http.server 8080
   ```

2. Navigate to the frontend directory:
   ```bash
   cd frontend
   ```

3. Start the web server:
   ```bash
   # Using http-server
   http-server -p 8080 -c-1
   
   # Or using Python
   python -m http.server 8080
   ```

4. Open your browser and go to `http://localhost:8080`

### Option 3: VS Code Live Server
1. Install the "Live Server" extension in VS Code
2. Open the frontend folder in VS Code
3. Right-click on `index.html` and select "Open with Live Server"

## Configuration

### API Endpoint Configuration
The frontend is configured to connect to the backend API at `http://localhost:5000/api`. To change this:

1. Edit `js/script.js`
2. Modify the `apiBaseUrl` property in the `ImageCompressionApp` constructor:
   ```javascript
   this.apiBaseUrl = 'https://your-api-domain.com/api';
   ```

### File Size and Type Limits
Default limits can be modified in `js/script.js`:
```javascript
this.maxFileSize = 10 * 1024 * 1024; // 10MB
this.allowedTypes = ['image/jpeg', 'image/png', 'image/webp', 'image/bmp'];
```

## Usage Guide

### 1. Upload Images
- **Drag & Drop**: Drag images directly onto the upload zone
- **Browse Files**: Click the "Browse Files" button to select images
- **Multiple Files**: Select multiple images at once for batch processing

### 2. Configure Compression Settings
- **Quality Slider**: Adjust compression quality (1-100)
  - Higher values = better quality, larger file size
  - Lower values = lower quality, smaller file size
- **Output Format**: Choose output format or keep original

### 3. Process Images
- Click "Compress Images" to start processing
- Monitor progress in real-time
- View processing status for each file

### 4. Review Results
- Compare original vs compressed images side-by-side
- View compression statistics (file sizes, percentage saved)
- Download compressed images individually

### 5. Download Compressed Images
- Click "Download Compressed" button for each result
- Files are automatically renamed with "_compressed" suffix

## File Structure

```
frontend/
├── index.html          # Main HTML structure
├── css/
│   └── style.css      # Styling and responsive design
├── js/
│   └── script.js      # Core JavaScript functionality
├── assets/            # Static assets (optional)
│   ├── icons/         # UI icons
│   └── images/        # Sample images
└── README.md          # This file
```

## Technical Details

### Browser Compatibility
- Chrome 60+ (recommended)
- Firefox 55+
- Safari 11+
- Edge 79+

### JavaScript Features Used
- ES6+ Classes and modules
- Async/await for API calls
- File API for drag-and-drop
- FormData for multipart uploads
- Fetch API for HTTP requests
- CSS Grid and Flexbox for layout

### API Integration
The frontend communicates with the backend via REST API:
- `POST /api/images/compress` - Compress images
- `GET /api/images/download/{id}` - Download compressed images

### Error Handling
- File type validation
- File size validation
- Network error handling
- API error response handling
- User-friendly error messages

## Customization

### Styling
Modify `css/style.css` to customize:
- Color scheme (CSS custom properties)
- Layout and spacing
- Typography
- Responsive breakpoints

### Functionality
Extend `js/script.js` to add:
- Additional file format support
- Custom compression presets
- Batch processing options
- Progress persistence

### UI Components
Add new features by modifying:
- `index.html` for new UI elements
- `css/style.css` for styling
- `js/script.js` for functionality

## Performance Considerations

- Large files are processed asynchronously
- Progress updates prevent UI blocking
- Image previews are optimized for display
- Temporary URLs are cleaned up automatically

## Security Notes

- Client-side validation only (server validation required)
- No sensitive data stored locally
- CORS policies respected
- File type validation using MIME types

## Troubleshooting

### Common Issues

1. **Images not uploading**
   - Check file size (must be < 10MB)
   - Verify file type (JPEG, PNG, WebP, BMP only)
   - Ensure backend API is running

2. **Compression not working**
   - Verify API endpoint configuration
   - Check browser developer console for errors
   - Ensure backend service is accessible

3. **Download not working**
   - Check browser popup blocker settings
   - Verify backend download endpoint is accessible
   - Try right-click "Save As" on download links

4. **UI not responsive**
   - Clear browser cache
   - Verify CSS file is loading
   - Check browser compatibility

### Debug Mode
Enable debug mode by opening browser developer tools:
- Console tab shows detailed error messages
- Network tab shows API request/response details
- Elements tab allows UI inspection

## Development

### Local Development
1. Make changes to HTML, CSS, or JavaScript files
2. Refresh browser to see changes
3. Use browser developer tools for debugging

### Production Deployment
1. Minify CSS and JavaScript files
2. Optimize images and assets
3. Configure proper CORS headers on backend
4. Set up HTTPS for production use

## Support

For issues related to:
- **Frontend functionality**: Check browser console for errors
- **Backend connectivity**: Verify API endpoint configuration
- **File processing**: See backend README for FFmpeg setup

## License

This project is part of the Image Compression Web App assignment. 