# EU Legal Document Search Platform

A comprehensive web application for searching, accessing, and downloading European Union legal documents. Built with Vue.js frontend and ASP.NET Core Web API backend.

## Features

- **Advanced Search**: Keyword-based search across EU legal documents
- **Category Filtering**: Filter by document type (directives, regulations, decisions)
- **Multi-language Support**: Access documents in multiple EU languages
- **PDF Viewing**: View documents directly in browser
- **Bulk Downloads**: Download ZIP archives of documents by language
- **Responsive Design**: Optimized for desktop and mobile devices

## Tech Stack

**Tech Stack:**
- Vue.js 3 with TypeScript
- Vue Router for navigation
- Pinia for state management
- Vite for build tooling
- Axios for HTTP requests

**Backend:**
- ASP.NET Core Web API (Clean Architecture)
- C#
- Entity Framework Core
- PostgreSQL
- JWT Authentication

## Project Structure

```
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
    │   └── Infrastructure/         # Data access layer
    │       ├── Clients/
    │       ├── Data/
    │       ├── Migrations/
    │       └── Repositories/
    └── frontend/                   # Vue.js application
        ├── public/
        └── src/
            ├── assets/
            ├── components/
            ├── router/
            └── store/
```

## Prerequisites

- **Node.js** (v24.8.0 or higher)
- **.NET 9.0 SDK** or higher
- **PostgreSQL**
- **Git**

## Getting Started

### 1. Clone the Repository

```bash
git clone https://github.com/novosel2/lawpicker-app.git
cd lawpicker-app
```

### 2. Backend Setup

```bash
cd src/api/
dotnet restore
dotnet build
```

### Backend Configuration

Configure user secrets for development:
```bash
cd src/api/Api
dotnet user-secrets init
dotnet user-secrets set "ConnectionStrings:AppConnection" "Host=localhost;Database=laws-db;Username=postgres;Password=yourpassword"
dotnet user-secrets set "ConnectionStrings:AuthConnection" "Host=localhost;Database=auth-laws-db;Username=postgres;Password=yourpassword"
dotnet user-secrets set "Jwt:Key" "your-development-jwt-secret-key"
dotnet user-secrets set "Jwt:Issuer" "https://localhost:8000"
dotnet user-secrets set "Jwt:Audience" "https://localhost:8000"
```

**Configure Database:**
1. Ensure PostgreSQL is running
2. Run database migrations:
```bash
dotnet ef database update --project Infrastructure --startup-project Api --context AppDbContext
dotnet ef database update --project Infrastructure --startup-project Api --context AuthDbContext
```

**Start the API:**
```bash
cd src/api/Api
dotnet run
```
API will be available at `https://localhost:8000` and `http://localhost:7999`

### 3. Frontend Setup

```bash
cd src/frontend
npm install
npm run dev
```
Frontend will be available at `http://localhost:3000`

## Configuration



### Frontend Configuration

Create `.env.local` in the `frontend` folder:
```
VITE_API_BASE_URL=https://localhost:8000/api
```

## API Endpoints

| Method |                                Endpoint                                      |          Description         |
|--------|------------------------------------------------------------------------------|------------------------------|
|  GET   | `/api/documents?search={searchQuery?}&documentTypes={"LDR"}&page=0&limit=10` | Search documents by keywords |
|  GET   |                       `/api/documents/{celex}/pdf`                           |      Get PDF by language     |
|  POST  |                           `/api/documents/pdf`                               |     Generate ZIP download    |
|--------|------------------------------------------------------------------------------|------------------------------|
