// Image Compression App - Frontend JavaScript
class ImageCompressionApp {
    constructor() {
        this.apiBaseUrl = 'https://imaginur-image-compression-backend.onrender.com/api';
        this.maxFileSize = 10 * 1024 * 1024; // 10MB
        this.allowedTypes = ['image/jpeg', 'image/png', 'image/webp', 'image/bmp'];
        this.selectedFiles = [];
        this.currentResults = [];
        
        this.initializeElements();
        this.setupEventListeners();
        this.setupDragAndDrop();
    }

    initializeElements() {
        // Main UI elements
        this.dragDropZone = document.getElementById('dragDropZone');
        this.dragActive = document.getElementById('dragActive');
        this.fileInput = document.getElementById('fileInput');
        this.browseBtn = document.getElementById('browseBtn');
        
        // Sections
        this.processingSection = document.getElementById('processingSection');
        this.settingsSection = document.getElementById('settingsSection');
        this.resultsSection = document.getElementById('resultsSection');
        
        // Progress elements
        this.progressFill = document.getElementById('progressFill');
        this.progressText = document.getElementById('progressText');
        this.processingFiles = document.getElementById('processingFiles');
        
        // Settings elements
        this.qualitySlider = document.getElementById('qualitySlider');
        this.qualityValue = document.getElementById('qualityValue');
        this.formatSelect = document.getElementById('formatSelect');
        this.compressBtn = document.getElementById('compressBtn');
        
        // Results elements
        this.resultsGrid = document.getElementById('resultsGrid');
        
        // Error and loading elements
        this.errorMessage = document.getElementById('errorMessage');
        this.errorText = document.getElementById('errorText');
        this.errorClose = document.getElementById('errorClose');
        this.loadingSpinner = document.getElementById('loadingSpinner');
    }

    setupEventListeners() {
        // File input and browse button
        this.browseBtn.addEventListener('click', (e) => {
            e.stopPropagation(); // Prevents the event from reaching the parent
            this.fileInput.click();
        });
        
        this.fileInput.addEventListener('change', (e) => {
            this.handleFiles(e.target.files);
        });
        
        // Quality slider
        this.qualitySlider.addEventListener('input', (e) => {
            this.qualityValue.textContent = e.target.value;
        });
        
        // Compress button
        this.compressBtn.addEventListener('click', () => {
            this.compressImages();
        });
        
        // Error message close
        this.errorClose.addEventListener('click', () => {
            this.hideError();
        });
        
        // Drag and drop zone click
        this.dragDropZone.addEventListener('click', () => {
            this.fileInput.click();
        });
    }

    setupDragAndDrop() {
        // Prevent default drag behaviors
        ['dragenter', 'dragover', 'dragleave', 'drop'].forEach(eventName => {
            this.dragDropZone.addEventListener(eventName, this.preventDefaults, false);
            document.body.addEventListener(eventName, this.preventDefaults, false);
        });

        // Highlight drop zone when item is dragged over it
        ['dragenter', 'dragover'].forEach(eventName => {
            this.dragDropZone.addEventListener(eventName, () => {
                this.dragDropZone.classList.add('drag-over');
                this.dragActive.classList.add('active');
            }, false);
        });

        ['dragleave', 'drop'].forEach(eventName => {
            this.dragDropZone.addEventListener(eventName, () => {
                this.dragDropZone.classList.remove('drag-over');
                this.dragActive.classList.remove('active');
            }, false);
        });

        // Handle dropped files
        this.dragDropZone.addEventListener('drop', (e) => {
            const dt = e.dataTransfer;
            const files = dt.files;
            this.handleFiles(files);
        }, false);
    }

    preventDefaults(e) {
        e.preventDefault();
        e.stopPropagation();
    }

    handleFiles(files) {
        const validFiles = [];
        const errors = [];

        Array.from(files).forEach(file => {
            if (!this.allowedTypes.includes(file.type)) {
                errors.push(`${file.name}: Unsupported file type. Please use JPEG, PNG, WebP, or BMP.`);
                return;
            }

            if (file.size > this.maxFileSize) {
                errors.push(`${file.name}: File too large. Maximum size is ${this.formatFileSize(this.maxFileSize)}.`);
                return;
            }

            validFiles.push(file);
        });

        if (errors.length > 0) {
            this.showError(errors.join('\n'));
        }

        if (validFiles.length > 0) {
            this.selectedFiles = validFiles;
            this.showSettingsSection();
            this.previewFiles(validFiles);
        }
    }

    showSettingsSection() {
        this.settingsSection.style.display = 'block';
        this.settingsSection.scrollIntoView({ behavior: 'smooth' });
    }

    previewFiles(files) {
        // Clear previous processing files
        this.processingFiles.innerHTML = '';

        files.forEach((file, index) => {
            const fileElement = this.createFilePreviewElement(file, index);
            this.processingFiles.appendChild(fileElement);
        });
    }

    createFilePreviewElement(file, index) {
        const fileElement = document.createElement('div');
        fileElement.className = 'processing-file';
        fileElement.innerHTML = `
            <div class="processing-file-icon">
                ${this.getFileIcon(file.type)}
            </div>
            <div class="processing-file-info">
                <div class="processing-file-name">${file.name}</div>
                <div class="processing-file-status">${this.formatFileSize(file.size)} • Ready for compression</div>
            </div>
        `;
        return fileElement;
    }

    getFileIcon(mimeType) {
        switch (mimeType) {
            case 'image/jpeg':
                return 'JPG';
            case 'image/png':
                return 'PNG';
            case 'image/webp':
                return 'WEBP';
            case 'image/bmp':
                return 'BMP';
            default:
                return 'IMG';
        }
    }

    async compressImages() {
        if (this.selectedFiles.length === 0) {
            this.showError('Please select images to compress.');
            return;
        }

        this.showProcessingSection();
        this.compressBtn.disabled = true;
        this.currentResults = [];

        const quality = this.qualitySlider.value;
        const format = this.formatSelect.value;

        try {
            for (let i = 0; i < this.selectedFiles.length; i++) {
                const file = this.selectedFiles[i];
                await this.compressFile(file, quality, format, i);
            }
            
            this.showResultsSection();
        } catch (error) {
            this.showError(`Compression failed: ${error.message}`);
        } finally {
            this.compressBtn.disabled = false;
            this.hideProcessingSection();
        }
    }

    showProcessingSection() {
        this.processingSection.style.display = 'block';
        this.processingSection.scrollIntoView({ behavior: 'smooth' });
    }

    hideProcessingSection() {
        this.processingSection.style.display = 'none';
    }

    async compressFile(file, quality, format, index) {
        const formData = new FormData();
        formData.append('file', file);
        formData.append('quality', quality);
        if (format !== 'same') {
            formData.append('format', format);
        }

        // Update progress for this file
        this.updateFileProgress(index, 0, 'Uploading...');

        try {
            const response = await fetch(`${this.apiBaseUrl}/image/compress`, {
                method: 'POST',
                body: formData
            });

            if (!response.ok) {
                const errorData = await response.json();
                throw new Error(errorData.error?.message || 'Compression failed');
            }

            // Simulate progress updates
            for (let progress = 25; progress <= 100; progress += 25) {
                this.updateFileProgress(index, progress, 
                    progress === 100 ? 'Completed' : 'Processing...');
                await this.delay(200);
            }

            const result = await response.json();
            
            if (result.success) {
                this.currentResults.push({
                    file: file,
                    result: result.data,
                    index: index
                });
            } else {
                throw new Error(result.error?.message || 'Compression failed');
            }

        } catch (error) {
            this.updateFileProgress(index, 0, `Error: ${error.message}`);
            throw error;
        }
    }

    updateFileProgress(index, progress, status) {
        const fileElements = this.processingFiles.children;
        if (fileElements[index]) {
            const statusElement = fileElements[index].querySelector('.processing-file-status');
            if (statusElement) {
                statusElement.textContent = status;
            }
        }

        // Update overall progress
        const overallProgress = ((index + (progress / 100)) / this.selectedFiles.length) * 100;
        this.progressFill.style.width = `${overallProgress}%`;
        this.progressText.textContent = `Processing ${index + 1} of ${this.selectedFiles.length}`;
    }

    showResultsSection() {
        this.resultsSection.style.display = 'block';
        this.resultsGrid.innerHTML = '';

        this.currentResults.forEach(({ file, result }) => {
            const resultElement = this.createResultElement(file, result);
            this.resultsGrid.appendChild(resultElement);
        });

        this.resultsSection.scrollIntoView({ behavior: 'smooth' });
    }

    createResultElement(originalFile, result) {
        const compressionRatio = ((1 - result.compressedSize / result.originalSize) * 100).toFixed(1);
        
        const resultElement = document.createElement('div');
        resultElement.className = 'result-item';
        resultElement.innerHTML = `
            <div class="result-header">
                <div class="result-filename">${originalFile.name}</div>
                <div class="result-stats">
                    <span>Original: ${this.formatFileSize(result.originalSize)}</span>
                    <span>Compressed: ${this.formatFileSize(result.compressedSize)}</span>
                    <span>Saved: ${compressionRatio}%</span>
                </div>
            </div>
            <div class="result-images">
                <div class="result-image">
                    <img src="${URL.createObjectURL(originalFile)}" alt="Original">
                    <div class="result-image-label">Original (${this.formatFileSize(result.originalSize)})</div>
                </div>
                <div class="result-image">
                    <img src="${this.apiBaseUrl}${result.downloadUrl}" alt="Compressed">
                    <div class="result-image-label">Compressed (${this.formatFileSize(result.compressedSize)})</div>
                </div>
            </div>
            <div class="result-actions">
                <button class="download-btn" onclick="app.downloadFile('${result.downloadUrl}', '${originalFile.name}')">
                    Download Compressed
                </button>
            </div>
        `;

        return resultElement;
    }

    async downloadFile(downloadUrl, originalFileName) {
        try {
            const response = await fetch(`${this.apiBaseUrl}${downloadUrl}`);
            if (!response.ok) {
                throw new Error('Download failed');
            }

            const blob = await response.blob();
            const url = window.URL.createObjectURL(blob);
            const a = document.createElement('a');
            a.style.display = 'none';
            a.href = url;
            a.download = this.getCompressedFileName(originalFileName);
            document.body.appendChild(a);
            a.click();
            window.URL.revokeObjectURL(url);
            document.body.removeChild(a);
        } catch (error) {
            this.showError(`Download failed: ${error.message}`);
        }
    }

    getCompressedFileName(originalName) {
        const lastDotIndex = originalName.lastIndexOf('.');
        if (lastDotIndex === -1) {
            return `${originalName}_compressed`;
        }
        
        const name = originalName.substring(0, lastDotIndex);
        const extension = originalName.substring(lastDotIndex);
        return `${name}_compressed${extension}`;
    }

    formatFileSize(bytes) {
        if (bytes === 0) return '0 B';
        
        const k = 1024;
        const sizes = ['B', 'KB', 'MB', 'GB'];
        const i = Math.floor(Math.log(bytes) / Math.log(k));
        
        return parseFloat((bytes / Math.pow(k, i)).toFixed(2)) + ' ' + sizes[i];
    }

    showError(message) {
        this.errorText.textContent = message;
        this.errorMessage.style.display = 'block';
        
        // Auto-hide after 5 seconds
        setTimeout(() => {
            this.hideError();
        }, 5000);
    }

    hideError() {
        this.errorMessage.style.display = 'none';
    }

    delay(ms) {
        return new Promise(resolve => setTimeout(resolve, ms));
    }

    // Reset application state
    reset() {
        this.selectedFiles = [];
        this.currentResults = [];
        this.fileInput.value = '';
        this.processingFiles.innerHTML = '';
        this.resultsGrid.innerHTML = '';
        this.settingsSection.style.display = 'none';
        this.processingSection.style.display = 'none';
        this.resultsSection.style.display = 'none';
        this.progressFill.style.width = '0%';
        this.compressBtn.disabled = false;
    }
}

// Initialize the app when DOM is loaded
document.addEventListener('DOMContentLoaded', () => {
    window.app = new ImageCompressionApp();
});

// Global error handler
window.addEventListener('unhandledrejection', (event) => {
    console.error('Unhandled promise rejection:', event.reason);
    if (window.app) {
        window.app.showError('An unexpected error occurred. Please try again.');
    }
});

// Handle online/offline status
window.addEventListener('online', () => {
    if (window.app) {
        window.app.hideError();
    }
});

window.addEventListener('offline', () => {
    if (window.app) {
        window.app.showError('You are offline. Please check your internet connection.');
    }
}); 