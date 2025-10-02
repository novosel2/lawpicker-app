# EU Legal Document Search Platform

A comprehensive web application for searching, accessing, and downloading European Union legal documents. Built with Vue.js frontend and ASP.NET Core Web API backend, fully containerized with Docker.

## Features

- **Advanced Search**: Keyword-based search across EU legal documents
- **Category Filtering**: Filter by document type (directives, regulations, decisions)  
- **Multi-language Support**: Access documents in multiple EU languages
- **PDF Viewing**: View documents directly in browser
- **Bulk Downloads**: Download ZIP archives of documents by language
- **Responsive Design**: Optimized for desktop and mobile devices

## Tech Stack

**Frontend:**
- Vue.js 3 with TypeScript
- Vue Router for navigation
- Pinia for state management
- Vite for build tooling
- Axios for HTTP requests
- Nginx (containerized web server)

**Backend:**
- ASP.NET Core Web API (Clean Architecture)
- C# with .NET 9.0
- Entity Framework Core
- JWT Authentication

**Infrastructure:**
- PostgreSQL (containerized database)
- Redis (containerized cache)
- Azure Blob Storage (PDF storage)
- Docker & Docker Compose

## Project Structure

```
lawpicker-app/
├── backend/                    # Backend (.NET Clean Architecture)
│   ├── Api/                    # Web API layer
│   ├── Application/            # Application logic layer
│   ├── Domain/                 # Domain entities
│   ├── Infrastructure/         # Data access layer
│   └── Dockerfile
├── frontend/                   # Vue.js application
│   ├── src/
│   ├── Dockerfile
│   └── nginx.conf
├── docker-compose.yml
├── init-multiple-databases.sh
├── .env.example
└── README.md
```

## Quick Start

### Prerequisites
- Docker and Docker Compose
- Git

### Setup

```bash
# Clone repository
git clone https://github.com/novosel2/lawpicker-app.git
cd lawpicker-app

# Configure environment
cp .env.example .env
nvim .env  # Change POSTGRES_PASSWORD and JWT_SECRET_KEY, optionally add Azure Blob Storage

# Windows only: make script executable in WSL
chmod +x init-multiple-databases.sh

# Start application
docker-compose up --build
```

### Access
- **Frontend**: http://localhost:3000
- **Backend API**: http://localhost:8000
- **Swagger**: http://localhost:8000/swagger

## Development Workflow

### Making Backend Changes

```bash
# After editing C# code:
docker-compose build backend
docker-compose up -d backend
```

### Making Frontend Changes

```bash
# After editing Vue.js code:
docker-compose build frontend  
docker-compose up -d frontend
```

### Viewing Logs

```bash
# View logs for all services
docker-compose logs -f

# View logs for specific service
docker-compose logs -f backend
docker-compose logs -f frontend
docker-compose logs -f postgres
```

### Stopping the Application

```bash
# Stop all services (preserves data)
docker-compose down

# Stop and remove all data (fresh start)
docker-compose down -v
```

### Restarting Services

```bash
# Restart a specific service
docker-compose restart backend

# Restart all services
docker-compose restart
```

## API Endpoints

| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/api/documents?search={query}&documentTypes={types}&page=0&limit=10` | Search documents |
| GET | `/api/documents/{celex}/pdf?lang={lang}` | Get PDF by language |
| POST | `/api/documents/bulk-pdf` | Generate ZIP download |
| POST | `/api/auth/register` | Register new user |
| POST | `/api/auth/login` | User login |

## Container Architecture

The application runs across 4 containers:

- **Frontend (nginx)**: Serves Vue.js app and proxies API calls
- **Backend (.NET)**: API server with business logic
- **PostgreSQL**: Database for documents and user authentication  
- **Redis**: Cache for PDF URLs and session data

All containers communicate through a dedicated Docker network and share data through named volumes.

## Troubleshooting

### Port Conflicts

If ports 3000, 5432, 6379, or 8000 are already in use, modify the port mappings in `docker-compose.yml`:

```yaml
services:
  frontend:
    ports:
      - "3001:80"  # Change 3000 to 3001 or another available port
```

### Database Connection Issues

```bash
# Check database logs
docker-compose logs postgres

# Restart database service
docker-compose restart postgres

# Verify database containers are running
docker-compose ps
```

### Build Issues

```bash
# Clean rebuild everything
docker-compose down -v
docker-compose build --no-cache
docker-compose up
```

### Permission Denied on init-multiple-databases.sh

If you encounter permission errors:
```bash
# On Windows (in WSL):
chmod +x init-multiple-databases.sh

# On Linux/Mac:
sudo chmod +x init-multiple-databases.sh
```

## Production Deployment

For production deployment:

1. Update environment variables with production values
2. Use proper SSL certificates
3. Configure database backups
4. Set up monitoring and logging solutions
5. Use Docker Swarm or Kubernetes for orchestration
6. Configure Azure Blob Storage for PDF persistence

## License

This project is licensed under the MIT License - see the LICENSE file for details.
