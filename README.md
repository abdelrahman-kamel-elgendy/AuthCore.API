# 🔐 AuthCore.API

[![.NET](https://img.shields.io/badge/.NET-8.0-purple)](https://dotnet.microsoft.com)
[![PostgreSQL](https://img.shields.io/badge/PostgreSQL-14+-336791?logo=postgresql&logoColor=white)](https://www.postgresql.org)
[![JWT](https://img.shields.io/badge/Auth-JWT-orange)](https://jwt.io)
[![License](https://img.shields.io/badge/license-MIT-green)](LICENSE)

A production-ready authentication REST API built with **ASP.NET Core 8** and **PostgreSQL**. Handles everything from registration to token rotation with a clean layered architecture.

---

## Features

- **JWT Authentication** — short-lived access tokens (1 hour) signed with HS256
- **Refresh Token Rotation** — cryptographically random 64-byte tokens, rotated on every use, expire after 7 days
- **Role-Based Authorization** — `Admin` and `User` roles seeded automatically at startup
- **Global Exception Handling** — single middleware maps every exception type to a consistent JSON response
- **Environment Secrets** — all secrets live in `.env`, loaded via `DotNetEnv`, never committed to git
- **Unified Response Envelope** — every endpoint returns `ApiResponse<T>` with `success`, `message`, `data`, `errors`
- **Swagger UI** — interactive docs with Bearer token support at `/swagger`
- **Auto-Migration** — database migrates automatically on startup

---

## Project Structure

```
AuthCore.API/
├── Controllers/
│   └── AuthController.cs              # Register, Login, RefreshToken, Logout
├── Data/
│   └── ApplicationDbContext.cs        # EF Core + Identity DbContext
├── DTOs/
│   ├── Auth/
│   │   ├── AuthResponseDto.cs
│   │   └── RefreshTokenDto.cs
│   ├── LoginDto.cs
│   ├── RegisterDto.cs
│   └── UserDto.cs
├── Exceptions/
│   ├── ApiException.cs                # Abstract base (400–500)
│   ├── BadRequestException.cs
│   ├── ConflictException.cs
│   ├── ForbiddenException.cs
│   ├── NotFoundException.cs
│   ├── UnauthorizedException.cs
│   └── ValidationException.cs        # 400 with field-level error map
├── Middleware/
│   └── ExceptionHandlingMiddleware.cs
├── Models/
│   ├── ApiResponse.cs
│   ├── PagedList.cs
│   ├── PaginationMetadata.cs
│   └── UserModel.cs                   # IdentityUser + refresh token fields
├── Repositories/
│   ├── IAuthRepository.cs
│   └── AuthRepository.cs
├── Services/
│   ├── Interfaces/IAuthService.cs
│   └── AuthService.cs
├── Properties/launchSettings.json
├── .env.example                       # ✅ Template — safe to commit
├── appsettings.json
├── appsettings.Development.json
├── AuthCore.API.csproj
└── Program.cs
```

---

## Getting Started

### Prerequisites

- [.NET 8 SDK](https://dotnet.microsoft.com/download)
- [PostgreSQL 14+](https://www.postgresql.org/download/)
- EF Core CLI — install once with `dotnet tool install --global dotnet-ef`

---

### 1 — Clone & restore

```bash
git clone https://github.com/abdelrahman-kamel-elgendy/AuthCore.API.git
cd AuthCore.API
dotnet restore
```

### 2 — Configure secrets

```bash
cp .env.example .env
```

Edit `.env` with your values:

```env
ConnectionStrings__PostgreSQL=Host=localhost;Database=AuthCoreDB;Username=postgres;Password=your_password
JWT__ValidIssuer=http://localhost:5000
JWT__ValidAudience=http://localhost:4200
JWT__SecretKey=at-least-32-chars-long-random-secret!@#$%
```

> `.env` is gitignored and will never be committed. In production, set these as real environment variables on your server or container — no `.env` file needed.

The `__` separator maps to nested config: `JWT__SecretKey` → `JWT:SecretKey` in `IConfiguration`.

### 3 — Migrate & run

```bash
dotnet ef migrations add InitialCreate
dotnet ef database update
dotnet run
```

Open **http://localhost:5000/swagger** 🎉

> Migrations and role seeding also run automatically every startup.

---

## API Reference

All endpoints are under `/api/auth`.

### POST `/register`

```json
{
  "firstName": "John",
  "lastName":  "Doe",
  "username":  "johndoe",
  "email":     "john@example.com",
  "password":        "Secret@123",
  "confirmPassword": "Secret@123"
}
```

Optional fields: `phoneNumber`, `address`, `birthDate`, `profileURL`.

Returns `200` with a success message. Returns `409` if email or username is already taken, `400` on validation errors.

---

### POST `/login`

```json
{
  "email":    "john@example.com",
  "password": "Secret@123"
}
```

```json
{
  "success": true,
  "message": "Login successful.",
  "data": {
    "token":        "eyJhbGci...",
    "refreshToken": "abc123...",
    "expiration":   "2026-03-08T14:00:00Z",
    "userId":       "abc-123",
    "userName":     "johndoe",
    "email":        "john@example.com",
    "roles":        ["User"]
  }
}
```

Returns `401` for invalid credentials, unconfirmed email, or deactivated account. The error message is always `"Invalid email or password."` — never revealing which field was wrong.

---

### POST `/refresh-token`

```json
{ "refreshToken": "abc123..." }
```

Returns a new `token` + `refreshToken` pair. The old refresh token is immediately invalidated (rotation). Returns `401` if the token is invalid or expired.

---

### POST `/logout`  `🔒 Authorized`

```
Authorization: Bearer {access_token}
```

Revokes the server-side refresh token. The access token expires naturally after its 1-hour window.

---

## Response Format

Every endpoint returns the same envelope:

```json
{
  "success": true,
  "message": "...",
  "data":    { },
  "errors":  ["..."],
  "validationErrors": {
    "fieldName": ["error message"]
  }
}
```

`errors` and `validationErrors` are omitted when empty.

---

## Security

| Concern | Approach |
|---|---|
| Secrets | `.env` via DotNetEnv, gitignored |
| Passwords | PBKDF2 + salt (ASP.NET Identity default) |
| Access token | JWT HS256 · 1 hr · `ClockSkew = 0` |
| Refresh token | 64 random bytes · 7 days · rotated on every use |
| User enumeration | Login always returns the same error regardless of which field is wrong |
| Email confirmation | Required before login is allowed |
| Account lockout | 5 failed attempts → 15-minute lockout |
| Password policy | Min 8 chars, uppercase, lowercase, digit, special character |
| Error details | Stack traces only exposed in `Development` |

---

## Stack

| | |
|---|---|
| Framework | ASP.NET Core 8 |
| ORM | Entity Framework Core 8 |
| Database | PostgreSQL via Npgsql |
| Identity | ASP.NET Core Identity |
| Secrets | DotNetEnv 3.1 |
| Docs | Swashbuckle / Swagger 6.5 |

---

## Changelog

| Version | Notes |
|---|---|
| v2.1 | `.env` secrets via DotNetEnv |
| v2.0 | Refresh tokens · global exception middleware · layered architecture · PostgreSQL |
| v1.0 | Initial release — SQL Server, basic JWT |

---

## License

[MIT](LICENSE)
