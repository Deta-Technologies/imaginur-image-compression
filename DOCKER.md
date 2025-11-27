# Docker Setup Guide

This guide covers running the Imaginur Image Compression application using Docker and Docker Compose.

## Prerequisites

- Docker Desktop (Windows/Mac) or Docker Engine (Linux)
- Docker Compose v2.0 or higher
- Git (for cloning the repository)

## Quick Start

### Development Environment

1. **Clone and navigate to the repository**
   ```bash
   cd imaginur-image-compression
   ```

2. **Start the services**
   ```bash
   docker-compose up --build
   ```

3. **Access the application**
   - Frontend: http://localhost:8080
   - Backend API: http://localhost:5000
   - Swagger UI: http://localhost:5000/swagger

4. **Stop the services**
   ```bash
   docker-compose down
   ```

### Production Environment

1. **Start production services**
   ```bash
   docker-compose -f docker-compose.prod.yml up -d
   ```

2. **Access the application**
   - Backend API: http://localhost:10000

3. **Stop production services**
   ```bash
   docker-compose -f docker-compose.prod.yml down
   ```

## Docker Compose Files

### `docker-compose.yml` (Development)

Development configuration with:
- **Backend API** on port 5000
  - Built from local Dockerfile
  - Development environment with Swagger enabled
  - Volume mounts for temp files and logs
  - Health checks enabled
- **Frontend** on port 8080
  - Nginx serving static files
  - API proxy to avoid CORS issues
  - Auto-reloads on file changes (volume mounted)

### `docker-compose.prod.yml` (Production)

Production configuration with:
- **Backend API** on port 10000
  - Uses pre-built image from GitHub Container Registry
  - Production environment (no Swagger)
  - Resource limits configured
  - Named volumes for persistence
  - Enhanced health checks

## Environment Configuration

### Using .env File

1. Copy the example file:
   ```bash
   cp .env.example .env
   ```

2. Edit `.env` with your values:
   ```env
   ASPNETCORE_ENVIRONMENT=Development
   ImageCompression__MaxFileSizeBytes=10485760
   ImageCompression__MaxConcurrentOperations=5
   ```

3. Docker Compose automatically loads `.env` file

### Overriding Environment Variables

You can override any environment variable at runtime:

```bash
docker-compose up -e ImageCompression__MaxFileSizeBytes=20971520
```

## Service Management

### View Logs

```bash
# All services
docker-compose logs -f

# Specific service
docker-compose logs -f imagecompression-api
docker-compose logs -f imagecompression-frontend
```

### Restart Services

```bash
# Restart all
docker-compose restart

# Restart specific service
docker-compose restart imagecompression-api
```

### Rebuild After Code Changes

```bash
# Rebuild and restart
docker-compose up --build

# Rebuild specific service
docker-compose build imagecompression-api
docker-compose up -d imagecompression-api
```

### Check Service Health

```bash
# Check running containers
docker-compose ps

# Health check status
docker inspect --format='{{json .State.Health}}' imagecompression-api-dev | jq
```

## Volume Management

### Development Volumes

Volumes are mounted from your local filesystem:
- `./backend/ImageCompressionApi/wwwroot/temp` - Temporary uploaded files
- `./backend/ImageCompressionApi/logs` - Application logs
- `./frontend` - Frontend static files

### Production Volumes

Named volumes persist data:
```bash
# List volumes
docker volume ls | grep imagecompression

# Inspect volume
docker volume inspect imagecompression-temp

# Remove volumes (WARNING: deletes data)
docker-compose -f docker-compose.prod.yml down -v
```

## Networking

### Development Network

- Network name: `imaginur-dev-network`
- Services can communicate using service names
  - Backend: `http://imagecompression-api:5000`
  - Frontend: `http://imagecompression-frontend:80`

### Production Network

- Network name: `imaginur-network`
- Isolated from other Docker networks

### Testing Internal Connectivity

```bash
# From frontend to backend
docker exec imagecompression-frontend-dev curl http://imagecompression-api:5000/api/image/health

# From backend
docker exec imagecompression-api-dev curl http://localhost:5000/api/image/health
```

## Troubleshooting

### Port Already in Use

If ports are in use, modify the port mappings:

```yaml
ports:
  - "5001:5000"  # Changed from 5000:5000
```

Or stop conflicting services:
```bash
# Windows
netstat -ano | findstr :5000
taskkill /PID <PID> /F

# Linux/Mac
lsof -ti:5000 | xargs kill -9
```

### FFmpeg Not Working

1. Check if FFmpeg is installed in container:
   ```bash
   docker exec imagecompression-api-dev ffmpeg -version
   ```

2. Rebuild if needed:
   ```bash
   docker-compose build --no-cache imagecompression-api
   ```

### File Upload Failures

1. Check volume mounts:
   ```bash
   docker exec imagecompression-api-dev ls -la /app/wwwroot/temp
   ```

2. Ensure temp directory has write permissions:
   ```bash
   docker exec imagecompression-api-dev chmod 777 /app/wwwroot/temp
   ```

### Container Won't Start

1. Check logs:
   ```bash
   docker-compose logs imagecompression-api
   ```

2. Check container status:
   ```bash
   docker-compose ps
   docker inspect imagecompression-api-dev
   ```

3. Try rebuilding:
   ```bash
   docker-compose down
   docker-compose build --no-cache
   docker-compose up
   ```

### CORS Issues

If you see CORS errors in the browser:

1. **Development**: The nginx config proxies API requests to avoid CORS. Use `/api/` paths in your frontend code.

2. **Direct API access**: Update CORS origins in `appsettings.json` or environment variables.

### Out of Disk Space

Clean up Docker resources:

```bash
# Remove unused containers, networks, images
docker system prune -a

# Remove specific volumes
docker volume rm imagecompression-temp imagecompression-logs

# Remove all unused volumes
docker volume prune
```

## Production Deployment

### Building Production Image

1. Build the image:
   ```bash
   cd backend/ImageCompressionApi
   docker build -t imaginur-image-compression-api:latest .
   ```

2. Tag for registry:
   ```bash
   docker tag imaginur-image-compression-api:latest ghcr.io/deta-technologies/imaginur-image-compression-api:latest
   ```

3. Push to registry:
   ```bash
   docker push ghcr.io/deta-technologies/imaginur-image-compression-api:latest
   ```

### Deploying to Server

1. Copy `docker-compose.prod.yml` to server

2. Pull and start:
   ```bash
   docker-compose -f docker-compose.prod.yml pull
   docker-compose -f docker-compose.prod.yml up -d
   ```

3. Monitor logs:
   ```bash
   docker-compose -f docker-compose.prod.yml logs -f
   ```

## Performance Tuning

### Resource Limits

Adjust in `docker-compose.prod.yml`:

```yaml
deploy:
  resources:
    limits:
      cpus: '4'        # Maximum CPU cores
      memory: 4G       # Maximum memory
    reservations:
      cpus: '2'        # Reserved CPU cores
      memory: 1G       # Reserved memory
```

### Concurrent Operations

Increase processing capacity:

```env
ImageCompression__MaxConcurrentOperations=10
```

### File Upload Limits

```env
ImageCompression__MaxFileSizeBytes=52428800  # 50MB
```

## Security Best Practices

1. **Use secrets for sensitive data** (instead of .env):
   ```yaml
   secrets:
     - db_password
   ```

2. **Run as non-root user** (add to Dockerfile):
   ```dockerfile
   RUN useradd -m myuser
   USER myuser
   ```

3. **Scan images for vulnerabilities**:
   ```bash
   docker scan imaginur-image-compression-api:latest
   ```

4. **Keep images updated**:
   ```bash
   docker-compose pull
   docker-compose up -d
   ```

## Additional Commands

### Execute Commands in Container

```bash
# Open shell
docker exec -it imagecompression-api-dev bash

# Run one-off command
docker exec imagecompression-api-dev ls -la /app
```

### Copy Files To/From Container

```bash
# Copy from container
docker cp imagecompression-api-dev:/app/logs ./local-logs

# Copy to container
docker cp ./config.json imagecompression-api-dev:/app/
```

### Export/Import Images

```bash
# Export
docker save imaginur-image-compression-api:latest -o image-backup.tar

# Import
docker load -i image-backup.tar
```

## Support

For issues or questions:
- GitHub Issues: https://github.com/deta-technologies/imaginur-image-compression/issues
- Documentation: See CLAUDE.md for project architecture
