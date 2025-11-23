# Game Auth API

A production-ready authentication and authorization API built with .NET 9, featuring JWT tokens, refresh token rotation, and comprehensive security features.

## 🚀 Features

- **JWT Authentication** with refresh token rotation
- **Identity Management** with ASP.NET Core Identity
- **CQRS Pattern** with MediatR
- **FluentValidation** for request validation
- **Rate Limiting** per user and global
- **Security Headers** middleware
- **Health Checks** for monitoring
- **Structured Logging** with Serilog
- **OpenAPI/Swagger** with Scalar UI
- **Response Compression & Caching**
- **Database Auditing** with interceptors
- **Exception Handling** with ProblemDetails

## 🛠️ Tech Stack

- **.NET 9.0** - Latest .NET framework
- **Entity Framework Core 9.0** - ORM with SQLite
- **ASP.NET Core Identity** - User management
- **JWT Bearer Authentication** - Secure token-based auth
- **MediatR** - CQRS and mediator pattern
- **FluentValidation** - Input validation
- **Serilog** - Structured logging
- **Scalar** - Modern OpenAPI documentation

## 📋 Prerequisites

- [.NET 9.0 SDK](https://dotnet.microsoft.com/download/dotnet/9.0)
- SQLite (included)

## 🏃 Getting Started

### 1. Clone the repository

```bash
git clone <repository-url>
cd Rest
```

### 2. Configure environment

Copy the example environment file:

```bash
cp .env.example .env
```

Edit `.env` with your configuration:

```env
JWT_KEY=YourSuperSecretKeyHere
JWT_ISSUER=GameAuthApi
JWT_AUDIENCE=GameClient
JWT_EXPIRATION_HOURS=24
CONNECTION_STRING=Data Source=gameauth.db
SEED_DATA_ENABLED=true
```

### 3. Run the application

```bash
dotnet restore
dotnet run
```

The API will be available at:
- **API**: `http://localhost:5000`
- **API Docs**: `http://localhost:5000/scalar/v1`
- **Health Check**: `http://localhost:5000/health`

## 📚 API Endpoints

### Authentication

| Method | Endpoint | Description | Auth Required |
|--------|----------|-------------|---------------|
| POST | `/api/v1/auth/register` | Register new user | No |
| POST | `/api/v1/auth/login` | Login with credentials | No |
| POST | `/api/v1/auth/refresh` | Refresh access token | No |
| POST | `/api/v1/auth/logout` | Logout and revoke tokens | Yes |
| POST | `/api/v1/auth/change-password` | Change user password | Yes |

### Health

| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/health` | Health check endpoint |

## 🔒 Security Features

1. **JWT Token Security**
   - HMAC SHA256 signing
   - Configurable expiration
   - Claims-based authorization

2. **Refresh Token Rotation**
   - Automatic rotation on refresh
   - Reuse detection
   - Automatic cleanup of old tokens

3. **Security Headers**
   - X-Content-Type-Options
   - X-Frame-Options
   - X-XSS-Protection
   - Referrer-Policy
   - Strict-Transport-Security (HSTS)

4. **Rate Limiting**
   - Per-user rate limiting
   - Global rate limiting
   - Configurable limits

5. **Password Policy**
   - Minimum 8 characters
   - Requires uppercase, lowercase, digit, and special character
   - Account lockout after 5 failed attempts

## 🗄️ Database

The application uses SQLite with the following features:

- **Snake case naming convention** for database objects
- **Indexes** on frequently queried fields
- **Audit fields** (CreatedAt, LastLoginAt)
- **Migrations** for schema management

### Reset Database (Recommended)

Use the provided script to reset the database and apply migrations:

```bash
./reset-database.sh
```

### Manual Migrations

```bash
cd Rest
dotnet ef migrations add <MigrationName>
dotnet ef database update
```

For more details, see [DATABASE_MANAGEMENT.md](DATABASE_MANAGEMENT.md)

## 🧪 Testing

### Sample Requests

#### Register

```bash
curl -X POST http://localhost:5000/api/v1/auth/register \
  -H "Content-Type: application/json" \
  -d '{
    "username": "player1",
    "email": "player1@game.com",
    "password": "Password123!"
  }'
```

#### Login

```bash
curl -X POST http://localhost:5000/api/v1/auth/login \
  -H "Content-Type: application/json" \
  -d '{
    "username": "player1",
    "password": "Password123!"
  }'
```

#### Change Password (requires JWT token)

```bash
curl -X POST http://localhost:5000/api/v1/auth/change-password \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer <your-jwt-token>" \
  -d '{
    "currentPassword": "Password123!",
    "newPassword": "NewPassword123!",
    "confirmNewPassword": "NewPassword123!"
  }'
```

## 🔧 Configuration

### appsettings.json

Production configuration with secure defaults.

### appsettings.Development.json

Development configuration with verbose logging and seeded test data.

### User Secrets (Recommended for Development)

```bash
dotnet user-secrets set "Jwt:Key" "YourSecretKey"
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Data Source=gameauth.db"
```

## 📖 Project Structure

```
Rest/
├── Behaviors/          # MediatR pipeline behaviors
├── Common/            # Shared types (Result, Error)
├── Controllers/       # API controllers
├── Data/              # DbContext and interceptors
├── Exceptions/        # Custom exceptions
├── Extensions/        # Extension methods
├── Features/          # Feature-based organization (CQRS)
│   └── Auth/         # Authentication features
├── Middleware/        # Custom middleware
├── Migrations/        # EF Core migrations
├── Models/            # Domain entities
├── Options/           # Configuration options
└── Services/          # Application services
```

## 🎯 Best Practices Implemented

1. **Clean Architecture** - Feature-based organization
2. **SOLID Principles** - Separation of concerns
3. **Dependency Injection** - Constructor injection
4. **Configuration Management** - Options pattern
5. **Error Handling** - Global exception handler with ProblemDetails
6. **Validation** - FluentValidation with pipeline behavior
7. **Logging** - Structured logging with Serilog
8. **Security** - Defense in depth approach
9. **Performance** - Response caching and compression
10. **Monitoring** - Health checks and metrics

## 🚀 Deployment

### Production Checklist

- [ ] Change JWT secret key (use Key Vault or secrets manager)
- [ ] Update CORS policy to specific origins
- [ ] Enable HTTPS enforcement
- [ ] Configure proper logging (Application Insights, etc.)
- [ ] Set up proper database (PostgreSQL, SQL Server)
- [ ] Configure rate limiting for production load
- [ ] Enable response caching strategies
- [ ] Set up monitoring and alerting
- [ ] Review security headers
- [ ] Disable database seeding

### Docker Support

```dockerfile
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
COPY ["Rest/Rest.csproj", "Rest/"]
RUN dotnet restore "Rest/Rest.csproj"
COPY . .
WORKDIR "/src/Rest"
RUN dotnet build "Rest.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "Rest.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Rest.dll"]
```

## 📝 License

MIT License - See LICENSE file for details

## 🤝 Contributing

Contributions are welcome! Please follow these steps:

1. Fork the repository
2. Create a feature branch
3. Commit your changes
4. Push to the branch
5. Open a Pull Request

## 📞 Support

For issues and questions, please open an issue on GitHub.

---

**Built with ❤️ using .NET 9**
