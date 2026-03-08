# 🔐 AuthCore.API

[![.NET](https://img.shields.io/badge/.NET-8.0-purple)](https://dotnet.microsoft.com)
[![ASP.NET Core](https://img.shields.io/badge/ASP.NET-Core-blue)](https://learn.microsoft.com/aspnet/core)
[![PostgreSQL](https://img.shields.io/badge/PostgreSQL-16-336791?logo=postgresql&logoColor=white)](https://www.postgresql.org)
[![JWT](https://img.shields.io/badge/Auth-JWT-orange)](https://jwt.io)
[![License](https://img.shields.io/badge/license-MIT-green)](LICENSE)

**AuthCore.API** is a production-ready authentication REST API built with **ASP.NET Core 8** and **PostgreSQL**.  
It provides JWT access tokens, refresh token rotation, role-based authorization, and a clean layered architecture.

---

## ✨ Features

- 🔑 **JWT Authentication** — short-lived access tokens (1 hour)
- 🔄 **Refresh Token Rotation** — secure 7-day refresh tokens, rotated on every use
- 👤 **Role-Based Authorization** — `Admin` and `User` roles, seeded automatically
- 🛡️ **Global Exception Handling** — middleware maps all exceptions to consistent JSON responses
- 📦 **Layered Architecture** — Controller → Service → Repository
- 📄 **Swagger UI** — interactive API docs with Bearer token support
- 🐘 **PostgreSQL** via Entity Framework Core + auto-migration on startup

---

## 🏗️ Project Structure

```
AuthCore.API/
│
├── Controllers/
│   └── AuthController.cs          # Register, Login, RefreshToken, Logout
│
├── Data/
│   └── ApplicationDbContext.cs    # EF Core DbContext (Identity + custom columns)
│
├── DTOs/
│   ├── Auth/
│   │   ├── AuthResponseDto.cs     # Login / refresh response shape
│   │   └── RefreshTokenDto.cs     # Refresh token request
│   ├── LoginDto.cs                # Login request
│   ├── RegisterDto.cs             # Registration request (with validation)
│   └── UserDto.cs                 # User data transfer object
│
├── Exceptions/
│   ├── ApiException.cs            # Abstract base exception
│   ├── BadRequestException.cs     # 400
│   ├── ConflictException.cs       # 409
│   ├── ForbiddenException.cs      # 403
│   ├── NotFoundException.cs       # 404
│   ├── UnauthorizedException.cs   # 401
│   └── ValidationException.cs     # 400 with field-level errors
│
├── Middleware/
│   └── ExceptionHandlingMiddleware.cs  # Global error → JSON response
│
├── Models/
│   ├── ApiResponse.cs             # Unified response envelope
│   ├── PagedList.cs               # Generic pagination wrapper
│   ├── PaginationMetadata.cs      # Pagination info DTO
│   └── UserModel.cs               # IdentityUser + custom fields
│
├── Repositories/
│   ├── IAuthRepository.cs         # Repository contract
│   └── AuthRepository.cs          # Identity-backed implementation
│
├── Services/
│   ├── Interfaces/
│   │   └── IAuthService.cs        # Service contract
│   └── AuthService.cs             # Business logic
│
├── Properties/
│   └── launchSettings.json
│
├── appsettings.json
├── appsettings.Development.json
├── AuthCore.API.csproj
└── Program.cs                     # App bootstrap, DI, middleware pipeline
```

---

## 🚀 Getting Started

### Prerequisites

| Tool | Version |
|---|---|
| [.NET SDK](https://dotnet.microsoft.com/download) | 8.0+ |
| [PostgreSQL](https://www.postgresql.org/download/) | 14+ |
| [EF Core CLI](https://learn.microsoft.com/ef/core/cli/dotnet) | 8.0+ |

Install the EF Core CLI if you haven't:
```bash
dotnet tool install --global dotnet-ef
```

### 1 — Clone & restore

```bash
git clone https://github.com/abdelrahman-kamel-elgendy/AuthCore.API.git
cd AuthCore.API/AuthCore.API
dotnet restore
```

### 2 — Configure `appsettings.json`

Open `appsettings.json` and fill in your values:

```json
{
  "ConnectionStrings": {
    "PostgreSQL": "Host=localhost;Database=AuthCoreDB;Username=postgres;Password=YOUR_PASSWORD"
  },
  "JWT": {
    "ValidIssuer":  "http://localhost:5000",
    "ValidAudience": "http://localhost:4200",
    "SecretKey": "REPLACE_WITH_A_LONG_RANDOM_SECRET_MIN_32_CHARS"
  }
}
```

> ⚠️ **Never commit real secrets.** Use environment variables or User Secrets in production.

### 3 — Run migrations

```bash
dotnet ef migrations add InitialCreate
dotnet ef database update
```

> Migrations also run automatically on startup via `MigrateAsync()`.

### 4 — Run

```bash
dotnet run
```

Open **http://localhost:5000/swagger** to explore the API. 🎉

---

## 📡 API Reference

Base URL: `http://localhost:5000/api/auth`

### Register
```http
POST /api/auth/register
Content-Type: application/json

{
  "firstName": "John",
  "lastName": "Doe",
  "username": "johndoe",
  "email": "john@example.com",
  "password": "Secret@123",
  "confirmPassword": "Secret@123"
}
```

**Response `200 OK`:**
```json
{
  "success": true,
  "message": "Registration successful. Please check your email for confirmation.",
  "data": { "isSuccess": true, "message": "..." }
}
```

---

### Login
```http
POST /api/auth/login
Content-Type: application/json

{
  "email": "john@example.com",
  "password": "Secret@123"
}
```

**Response `200 OK`:**
```json
{
  "success": true,
  "data": {
    "token": "eyJhbGci...",
    "refreshToken": "abc123...",
    "expiration": "2025-01-01T13:00:00Z",
    "userId": "...",
    "userName": "johndoe",
    "email": "john@example.com",
    "roles": ["User"]
  }
}
```

---

### Refresh Token
```http
POST /api/auth/refresh-token
Content-Type: application/json

{
  "refreshToken": "abc123..."
}
```

Returns a new `token` + `refreshToken` pair. The old refresh token is invalidated.

---

### Logout
```http
POST /api/auth/logout
Authorization: Bearer {token}
```

Revokes the server-side refresh token. The access token expires naturally.

---

## 🔒 Security Design

| Concern | Implementation |
|---|---|
| Password storage | ASP.NET Identity (PBKDF2 + salt) |
| Access token | JWT, HS256, 1-hour expiry, `ClockSkew = 0` |
| Refresh token | Cryptographically random (64 bytes), 7-day expiry, rotated on every use |
| User enumeration | Login always returns `"Invalid email or password."` regardless of which is wrong |
| Unconfirmed accounts | Blocked from logging in until email is confirmed |
| Account lockout | 5 failed attempts → 15-minute lockout |
| Password rules | Min 8 chars, uppercase, lowercase, digit, special character |
| Stack trace exposure | Only shown in `Development` environment |

---

## 🗂️ Response Format

All endpoints return the same `ApiResponse<T>` envelope:

```json
{
  "success": true | false,
  "message": "Human-readable message",
  "data": { ... },
  "errors": ["error1", "error2"],
  "validationErrors": {
    "fieldName": ["error message"]
  }
}
```

`errors` and `validationErrors` are omitted (`null`) when not applicable.

---

## 🧰 Tech Stack

| Layer | Technology |
|---|---|
| Framework | ASP.NET Core 8 |
| ORM | Entity Framework Core 8 |
| Database | PostgreSQL 16 (via Npgsql) |
| Authentication | ASP.NET Core Identity + JWT Bearer |
| API Docs | Swashbuckle / Swagger |

---

## 📅 Changelog

| Version | Date | Notes |
|---|---|---|
| v2.0 | 2026-03-08 | Refresh tokens, global exception middleware, layered architecture, PostgreSQL |
| v1.0 | 2023-08-20 | Initial release (SQL Server, basic JWT) |
