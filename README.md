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
└── src/
    ├── backend/                    # Backend (.NET Clean Architecture)
    │   ├── Api/                    # Web API layer
    │   │   ├── Controllers/
    │   │   ├── Properties/
    │   │   ├── StartupExtensions/
    │   │   └── Filters/
    │   ├── Application/            # Application logic layer
    │   │   ├── Dto/
    │   │   ├── Exceptions/
    │   │   ├── Interfaces/
    │   │   └── Services/
    │   ├── Domain/                 # Domain entities
    │   │   └── Entities/
    │   ├── Infrastructure/         # Data access layer
    │   │   ├── Clients/
    │   │   ├── Data/
    │   │   ├── Migrations/
    │   │   └── Repositories/
    │   └── Dockerfile              # Backend container definition
    ├── frontend/                   # Vue.js application
    │   ├── public/
    │   ├── src/
    │   │   ├── assets/
    │   │   ├── components/
    │   │   ├── router/
    │   │   └── store/
    │   ├── Dockerfile              # Frontend container definition
    │   └── nginx.conf              # Web server configuration
    ├── docker-compose.yml          # Service orchestration
    ├── .env.example               # Environment template
    └── init-multiple-databases.sh # Database initialization
```

## Quick Start with Docker

### Prerequisites

- **Docker** and **Docker Compose** installed
- **Azure Blob Storage account** (for PDF storage)
- **Git**

### Setup

1. **Clone the repository:**
   ```bash
   git clone https://github.com/novosel2/lawpicker-app.git
   cd lawpicker-app/src
   ```

2. **Configure environment variables:**
   ```bash
   cp .env.example .env
   nano .env  # Edit with your configuration
   ```

3. **Set up your `.env` file:**
   ```env
   # PostgreSQL Configuration
   POSTGRES_USER=lawpicker_user
   POSTGRES_PASSWORD=your_secure_password
   APP_DB_NAME=laws-db
   AUTH_DB_NAME=auth-laws-db

   # JWT Configuration
   JWT_SECRET_KEY=your-jwt-secret-key-at-least-32-characters-long-jwt-secret-key-at-least-32-characters-long-jwt-secret-key-at-least-32-characters-long

   # Azure Blob Storage (Required)
   AZURE_BLOB_CONNECTION="DefaultEndpointsProtocol=https;AccountName=YOUR_ACCOUNT;AccountKey=YOUR_KEY;EndpointSuffix=core.windows.net"
   ```

4. **Start the application:**
   ```bash
   docker-compose up --build
   ```

### Access the Application

- **Frontend**: http://localhost:3000
- **Backend API**: http://localhost:8000
- **Swagger**: http://localhost:8000/swagger

## What Happens Automatically

When you run `docker-compose up --build`, the system will:

1. **Build all Docker images** (frontend, backend)
2. **Start all services** (PostgreSQL, Redis, Backend API, Frontend)
3. **Create databases** (laws-db and auth-laws-db)
4. **Run database migrations** (create all required tables)
5. **Seed database with EU legal documents** (if database is empty, this might take some time)

The application is ready to use once all containers are running and migrations are complete.

## Development Workflow

### Making Backend Changes

```bash
# After editing C# code:
docker-compose build backend
docker-compose up --no-deps backend
```

### Making Frontend Changes

```bash
# After editing Vue.js code:
docker-compose build frontend  
docker-compose up --no-deps frontend
```

### Viewing Logs

```bash
# View logs for all services
docker-compose logs

# View logs for specific service
docker-compose logs backend
docker-compose logs frontend
docker-compose logs postgres
```

### Stopping the Application

```bash
# Stop all services
docker-compose down

# Stop and remove all data (fresh start)
docker-compose down -v
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

## Production Deployment

For production deployment:

1. Update environment variables for production values
2. Use proper SSL certificates
3. Configure proper database backups
4. Set up monitoring and logging
5. Use Docker Swarm or Kubernetes for orchestration

## Troubleshooting

### Port Conflicts
If ports 3000, 5432, 6379, or 8000 are already in use, modify the port mappings in `docker-compose.yml`:

```yaml
services:
  frontend:
    ports:
      - "3001:80"  # Use different host port
```

### Database Issues
```bash
# Check database logs
docker-compose logs postgres

# Restart database service
docker-compose restart postgres
```

### Build Issues
```bash
# Clean rebuild everything
docker-compose down -v
docker-compose up --build --force-recreate
```

## License

This project is licensed under the MIT License - see the LICENSE file for details.
