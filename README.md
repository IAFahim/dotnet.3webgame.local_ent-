# 🎮 3Web Game Authentication API - Professional Edition

## Overview
This is a **production-ready** .NET 9.0 REST API implementing **enterprise-grade authentication** following industry best practices from Auth0, Microsoft, and OWASP guidelines.

### 🏆 Key Features
- ✅ **JWT Access Tokens** (15min) + **Refresh Tokens** (7 days)
- ✅ **Token Rotation** - Auto-refresh with revocation
- ✅ **BCrypt Password Hashing** with validation rules
- ✅ **HttpOnly Cookies** for XSS protection
- ✅ **IP Tracking** for security audit
- ✅ **Swagger/OpenAPI** with JWT support
- ✅ **Complete Test Coverage** (7 tests)
- ✅ **Register, Login, Logout, Refresh** endpoints

## Architecture
The implementation follows the UML sequence diagram:
```
Player → Auth API: POST /api/auth/login {username, password}
Auth API → Database: Validate credentials
Database → Auth API: User data (if valid)
Auth API → Database: Fetch coin status
Database → Auth API: Coin balance
Auth API → Player: {token, coinStatus}
```

## Project Structure
- **Rest/** - Main API project
  - **Controllers/** - API controllers
  - **Services/** - Business logic
  - **Data/** - Data access layer
  - **Models/** - Data models
- **Rest.Tests/** - xUnit test project

## 📋 API Endpoints

### 🔐 Authentication Endpoints

| Method | Endpoint | Auth Required | Description |
|--------|----------|---------------|-------------|
| POST | `/api/auth/register` | No | Register new user |
| POST | `/api/auth/login` | No | Login and get tokens |
| POST | `/api/auth/refresh-token` | No | Refresh access token |
| POST | `/api/auth/logout` | Yes | Revoke refresh token |
| GET | `/api/auth/coins/{username}` | Yes | Get user coin balance |

### Quick Examples

**1. Register:**
```json
POST /api/auth/register
{
  "username": "newplayer",
  "email": "player@game.com",
  "password": "Player123!"
}
```

**2. Login:**
```json
POST /api/auth/login
{
  "username": "testuser",
  "password": "password123"
}

Response:
{
  "accessToken": "eyJhbGci...",
  "refreshToken": "LZdKP9m8...",
  "accessTokenExpiry": "2024-11-21T10:00:00Z",
  "refreshTokenExpiry": "2024-11-28T09:45:00Z",
  "username": "testuser",
  "coinBalance": 1000.00
}
```

**3. Get Coins (Protected):**
```http
GET /api/auth/coins/testuser
Authorization: Bearer {accessToken}
```

📖 **[See Complete API Documentation](./AUTHENTICATION_GUIDE.md)**

## Test Users
| Username  | Password    | Coin Balance |
|-----------|-------------|--------------|
| testuser  | password123 | 1000.00      |
| player1   | player123   | 500.50       |

## Running the Application

### Build and Test
```bash
dotnet restore
dotnet build
dotnet test
```

### Run the API
```bash
cd Rest
dotnet run
```

The API will be available at `http://localhost:5000`

## 🧪 Testing
The project includes **7 professional unit tests**:

1. ✅ **Register** - Valid registration creates user with starting bonus
2. ✅ **Register** - Duplicate username/email rejected
3. ✅ **Login** - Valid credentials return tokens + coin balance
4. ✅ **Login** - Invalid password rejected
5. ✅ **Refresh** - Valid refresh token generates new tokens
6. ✅ **Logout** - Successfully revokes refresh token
7. ✅ **Get Coins** - Returns user coin balance

Run tests:
```bash
dotnet test
```

**All tests passing** ✅

## 🛠️ Technologies & Best Practices

### Core Stack
- **.NET 9.0** - Latest LTS framework
- **ASP.NET Core Web API** - RESTful API
- **JWT Bearer Authentication** - Stateless auth
- **BCrypt** - Industry-standard password hashing
- **Swagger/OpenAPI** - Interactive documentation

### Security Features
- ✅ Access Token (15min) + Refresh Token (7 days)
- ✅ Token rotation with revocation
- ✅ HttpOnly cookies for refresh tokens
- ✅ Strong password validation
- ✅ IP address tracking
- ✅ CORS configuration

### Testing
- **xUnit** - Modern testing framework
- **Moq** - Mocking library
- **7 comprehensive tests**

### Architecture Patterns
- ✅ Repository Pattern
- ✅ Service Layer Pattern
- ✅ Dependency Injection
- ✅ Interface Segregation
- ✅ Single Responsibility Principle
