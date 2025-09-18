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
    ├── backend/                        # Backend (.NET Clean Architecture)
    │   ├── Api/                    # Web API layer
    │   │   ├── Controllers/
    │   │   ├── Properties/
    │   │   └── StartupExtensions/
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

- **Node.js** (v16 or higher)
- **.NET 6.0 SDK** or higher
- **PostgreSQL**
- **Git**

## Getting Started

### 1. Clone the Repository

```bash
git clone https://github.com/yourusername/eu-legal-search.git
cd eu-legal-search
```

### 2. Backend Setup

```bash
cd src/api/Api
dotnet restore
dotnet build
```

**Configure Database:**
1. Ensure PostgreSQL is running
2. Create databases: `laws-db` and `auth-laws-db`
3. Run database migrations:
```bash
dotnet ef database update
```

**Start the API:**
```bash
cd src/api/Api
dotnet run
```
API will be available at `https://localhost:8000`

### 3. Frontend Setup

```bash
cd src/frontend
npm install
npm run dev
```
Frontend will be available at `http://localhost:3000`

## Configuration

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

### Frontend Configuration

Create `.env.local` in the `frontend` folder:
```
VITE_API_BASE_URL=https://localhost:8000/api
```

## API Endpoints

| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/api/documents/search` | Search documents by keywords |
| GET | `/api/documents/categories` | Get document categories |
| GET | `/api/documents/{id}/pdf` | Get PDF by language |
| POST | `/api/documents/download-zip` | Generate ZIP download |

## Development

### Running Tests

**Backend Tests:**
```bash
cd src/api/Api
dotnet test
```

**Frontend Tests:**
```bash
cd src/frontend
npm run test
```

### Code Style

- **Backend**: Follow C# coding conventions
- **Frontend**: ESLint + Prettier configuration included

## Deployment

### Production Build

**Frontend:**
```bash
cd src/frontend
npm run build
```

**Backend:**
```bash
cd src/api/Api
dotnet publish -c Release
```

### Environment Variables

Set the following environment variables in your production environment:

- `ConnectionStrings__AppConnection`: Main database connection string
- `ConnectionStrings__AuthConnection`: Authentication database connection string
- `Jwt__Key`: JWT secret key for token generation
- `Jwt__Issuer`: JWT token issuer (your domain)
- `Jwt__Audience`: JWT token audience (your domain)

Example:
```bash
ConnectionStrings__AppConnection="Host=your-host;Database=laws-db;Username=user;Password=pass"
ConnectionStrings__AuthConnection="Host=your-host;Database=auth-laws-db;Username=user;Password=pass"
Jwt__Key="your-very-secure-production-jwt-key"
Jwt__Issuer="https://yourapp.com"
Jwt__Audience="https://yourapp.com"
```

## Contributing

1. Fork the repository
2. Create a feature branch (`git checkout -b feature/amazing-feature`)
3. Commit your changes (`git commit -m 'Add amazing feature'`)
4. Push to the branch (`git push origin feature/amazing-feature`)
5. Open a Pull Request

### Development Guidelines

- Write tests for new features
- Follow existing code style conventions
- Update documentation for API changes
- Ensure all tests pass before submitting PR

## License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

## Support

If you encounter any issues or have questions:

1. Check the [Issues](https://github.com/yourusername/eu-legal-search/issues) section
2. Create a new issue with detailed description
3. Include steps to reproduce any bugs

## Acknowledgments

- European Union for providing public access to legal documents
- Vue.js and ASP.NET Core communities for excellent documentation
- Contributors and testers who helped improve this platform

## Roadmap

- [ ] Advanced search filters (date ranges, legal domains)
- [ ] User accounts and saved searches
- [ ] Document annotation and bookmarking
- [ ] API rate limiting and caching
- [ ] Mobile app development
- [ ] Integration with additional legal databases
