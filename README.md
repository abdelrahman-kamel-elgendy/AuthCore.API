<div align="center">

# 🔐 AuthCore.API

[![.NET](https://img.shields.io/badge/.NET-8.0-purple)](https://dotnet.microsoft.com)
[![PostgreSQL](https://img.shields.io/badge/PostgreSQL-14+-336791?logo=postgresql&logoColor=white)](https://www.postgresql.org)
[![Redis](https://img.shields.io/badge/Redis-7-DC382D?logo=redis&logoColor=white)](https://redis.io)
[![JWT](https://img.shields.io/badge/Auth-JWT-orange)](https://jwt.io)
[![Docker](https://img.shields.io/badge/Docker-ready-2496ED?logo=docker&logoColor=white)](https://www.docker.com)

A production-ready authentication REST API built with **ASP.NET Core 8** and **PostgreSQL**. Handles the full auth lifecycle, registration, email confirmation, login, JWT + refresh token rotation on logout, password management, user profiles, and admin controls.

</div>

---

## Features

- **JWT Authentication**: access tokens signed with HS256, expire after 1 hour with zero clock skew tolerance; refresh tokens are cryptographically random 64-byte values, rotated on every use and expired after 7 days
- **Redis Token Blacklist**: revoked access tokens are stored in Redis with automatic TTL expiration matching the token's remaining lifetime; every authenticated request checks Redis via an O(1) `EXISTS` call — no database query; keys are cleaned up automatically when the token would have expired naturally, with no maintenance job required
- **Refresh Token Rotation**: every `/refresh-token` call invalidates the old token and issues a new one; reuse of a consumed token is detected and rejected
- **Email Confirmation**: account login is blocked until the email address is verified; confirmation and welcome emails are sent automatically using dark-themed HTML templates
- **Forgot / Reset Password**: time-limited reset links sent via email; successful reset immediately revokes all active refresh tokens across all devices
- **Role-Based Authorization**: `Admin` and `User` roles enforced at the controller level via `[Authorize(Roles = "...")]`; roles and the default admin account are seeded on every startup
- **Admin Controls**: paginated user management with promote/demote, activate/deactivate, and hard delete; deactivation immediately revokes all tokens and blocks future logins
- **Rate Limiting**: per-IP fixed-window limits on sensitive endpoints (`5/min` login, `3/5min` register, `3/15min` forgot-password) using .NET 8's built-in `RateLimiter`; all rejections return a consistent `429` body with a `Retry-After` header
- **Security Headers**: `SecurityHeadersMiddleware` injects `HSTS`, `CSP`, `X-Frame-Options`, `X-Content-Type-Options`, `Referrer-Policy`, and `Permissions-Policy` on every response; CSP is relaxed automatically for Swagger in development
- **Structured Logging**: Serilog replaces the default logger with configurable sinks (Console, File, Seq); every request is logged with method, path, status code, duration, client IP, and user agent via `UseSerilogRequestLogging`; Swagger traffic is silenced at `Verbose` level
- **Health Checks**: `GET /health` reports live status of PostgreSQL and SMTP; returns `200` when all dependencies are healthy, `503` when any fail; response includes per-check status and duration in JSON
- **Docker Support**: multi-stage `Dockerfile` produces a minimal runtime image (~200MB); `docker-compose.yml` orchestrates API + PostgreSQL + Redis with health-check gating so the API never starts before all dependencies are ready
- **Strongly-Typed Configuration**: all environment variables are bound to validated settings classes (`JwtConfigs`, `SmtpConfigs`, `SeedConfigs`, `AppConfigs`, `RedisConfigs`) with `.ValidateOnStart()`; the app refuses to start if any required value is missing or invalid
- **Consistent API Envelope**: every response, including `401`, `429`, and validation errors, returns the same `ApiResponse<T>` JSON structure; no endpoint ever returns a blank body
- **Global Exception Handling**: `ExceptionHandlingMiddleware` maps all custom exception types to their correct HTTP status codes; stack traces are only exposed in `Development`
- **Account Security**: 5 failed login attempts trigger a 15-minute lockout; password policy enforces minimum length, mixed case, digits, and special characters
- **Proxy-Aware**: `ForwardedHeadersMiddleware` resolves real client IPs from `X-Forwarded-For` so rate limiting and logging work correctly behind Nginx or Cloudflare
- **Swagger UI**: fully documented with Bearer token support at `/swagger`; only exposed in `Development`

---

## Project Structure

```
AuthCore.API/
├── Controllers/
│   ├── AuthController.cs              # Register, Login, Logout, Confirm, ForgotPassword, ResetPassword
│   ├── UserController.cs              # GetProfile, UpdateProfile, ChangePassword
│   └── AdminController.cs             # GetAllUsers, GetUser, Promote, Demote, Activate, Deactivate, Delete
│
├── Configs/                           # Strongly-typed configuration classes
│   ├── AppConfigs.cs                  # App__BaseUrl
│   ├── JwtConfigs.cs                  # JWT__SecretKey, Issuer, Audience, expiry
│   ├── RedisConfigs.cs                # Redis__ConnectionString
│   ├── SmtpConfigs.cs                 # Smtp__Host, Port, credentials, SSL
│   └── SeedConfigs.cs                 # Seed__Admin__* values
│
├── Data/
│   ├── ApplicationDbContext.cs
│   ├── DbSeeder.cs                    # Admin seeder: runs on every startup (accepts SeedConfigs)
│   └── Migrations/                    # EF Core migrations
│
├── DTOs/
│   ├── Auth/
│   │   ├── AuthResponseDto.cs
│   │   ├── ConfirmEmailRequestDto.cs
│   │   ├── ForgotPasswordRequestDto.cs
│   │   ├── LoginRequestDto.cs
│   │   ├── RefreshTokenRequestDto.cs
│   │   ├── RegisterRequestDto.cs
│   │   └── ResetPasswordRequestDto.cs
│   └── User/
│       ├── UserResponseDto.cs
│       ├── ChangePasswordRequestDto.cs
│       └── UpdateProfileRequestDto.cs
│
├── Exceptions/
│   ├── ApiException.cs                # Abstract base
│   └── CustomExceptions.cs            # 400, 401, 403, 404, 409 exception types
│
├── HealthChecks/
│   └── SmtpHealthCheck.cs             # TCP probe — verifies SMTP host:port is reachable
│
├── Middlewares/
│   ├── ExceptionHandlingMiddleware.cs # Maps exceptions to consistent ApiResponse<T> errors
│   └── SecurityHeadersMiddleware.cs   # Injects security headers on every response
│
├── Models/
│   ├── ApiResponse.cs
│   ├── PagedList.cs
│   ├── PaginationMetadata.cs
│   └── UserModel.cs
│
├── Repositories/
│   ├── IAuthRepository.cs
│   └── AuthRepository.cs
│
├── Services/
│   ├── Interfaces/
│   │   ├── IAdminService.cs
│   │   ├── IAuthService.cs
│   │   ├── IEmailService.cs
│   │   ├── ITokenBlacklistService.cs  # RevokeAsync + IsRevokedAsync
│   │   └── IUserService.cs
│   ├── AdminService.cs
│   ├── AuthService.cs
│   ├── EmailService.cs
│   ├── EmailTemplateService.cs
│   └── TokenBlacklistService.cs       # Redis implementation — O(1) lookup, auto TTL expiry
│
├── Templates/
│   └── Email/
│       ├── ConfirmEmail.html          # Sent on register
│       ├── ResetPassword.html         # Sent on forgot-password
│       └── WelcomeEmail.html          # Sent after email confirmed
│
├── .env                               # ⚠️ Secrets: gitignored
├── .env.example                       # ✅ Template: safe to commit
├── .env.docker                        # ⚠️ Docker secrets: gitignored
├── .env.docker.example                # ✅ Docker template: safe to commit
├── .dockerignore
├── NuGet.config                       # Clears Windows-specific fallback paths for Docker builds
├── appsettings.json                   # Serilog configuration + app settings
├── AuthCore.API.csproj
├── docker-compose.yml
├── Dockerfile
└── Program.cs
```

---

## Getting Started

### Option A — Local development

#### Prerequisites
- [.NET 8 SDK](https://dotnet.microsoft.com/download)
- [PostgreSQL 14+](https://www.postgresql.org/download/)
- [Redis 7+](https://redis.io/download/) (or run via Docker: `docker run -d -p 6379:6379 redis:7-alpine`)
- EF Core CLI: `dotnet tool install --global dotnet-ef`

#### 1. Clone & restore
```bash
git clone https://github.com/abdelrahman-kamel-elgendy/AuthCore.API.git
cd AuthCore.API
dotnet restore
```

#### 2. Configure `.env`
```bash
cp .env.example .env
```

Fill in all values — database connection, Redis connection string, JWT secret, SMTP credentials, and seed admin details.

> **Gmail SMTP**: use an [App Password](https://myaccount.google.com/apppasswords), not your regular Gmail password. Enable 2FA first, then generate an app password under *Security → 2-Step Verification → App passwords*.

#### 3. Create the PostgreSQL database
```bash
psql -U postgres -c "CREATE DATABASE AuthCoreDB;"
```

#### 4. Apply migrations
```bash
dotnet ef database update
```

#### 5. Run
```bash
dotnet run
```

Open **http://localhost:5000/swagger** 🎉

> **First login**: use the admin credentials you set in `.env` under `Seed__Admin__*`.

---

### Option B — Docker (recommended)

#### Prerequisites
- [Docker Desktop](https://www.docker.com/products/docker-desktop)

#### 1. Clone
```bash
git clone https://github.com/abdelrahman-kamel-elgendy/AuthCore.API.git
cd AuthCore.API
```

#### 2. Configure `.env.docker`
```bash
cp .env.docker.example .env.docker
```

Fill in your values — the format matches `.env.example` but uses Docker-specific variable names.

#### 3. Start the stack
```bash
docker compose --env-file .env.docker up -d
```

This pulls PostgreSQL and Redis, builds the API image, runs migrations automatically, and seeds the admin account. The API will not start until PostgreSQL and Redis both pass their health checks.

Open **http://localhost:8080/swagger** 🎉

#### Useful Docker commands

```bash
# View live API logs
docker compose --env-file .env.docker logs -f api

# Check container status and health
docker compose --env-file .env.docker ps

# Rebuild after code changes
docker compose --env-file .env.docker build --no-cache
docker compose --env-file .env.docker up -d

# Inspect the Redis blacklist
docker compose exec redis redis-cli keys "blacklist:jti:*"

# Stop everything (keeps database and Redis data)
docker compose down

# Stop and wipe all volumes
docker compose down -v
```

---

## API Reference

### Auth: `api/auth`

| Method | Route | Auth | Rate Limit | Description |
|---|---|---|---|---|
| `POST` | `/register` | — | 3 / 5 min / IP | Register new account, sends confirmation email |
| `GET` | `/confirm-email?userId=&token=` | — | — | Confirm email via link, sends welcome email |
| `POST` | `/login` | — | 5 / 1 min / IP | Login, returns access + refresh token |
| `POST` | `/refresh-token` | — | 60 / 1 min / IP | Rotate refresh token |
| `POST` | `/logout` | Bearer | — | Blacklist access token in Redis + revoke refresh token |
| `POST` | `/forgot-password` | — | 3 / 15 min / IP | Send password reset link (always returns 200) |
| `POST` | `/reset-password` | — | — | Reset password, revokes all refresh tokens |

---

#### `POST /api/auth/register`
```json
{
  "firstName":       "John",
  "lastName":        "Doe",
  "username":        "johndoe",
  "email":           "john@example.com",
  "password":        "Secret@123",
  "confirmPassword": "Secret@123"
}
```
Optional: `phoneNumber`, `address`, `birthDate`, `profileURL`.

```json
{
  "status":  201,
  "success": true,
  "message": "Registration successful. Please check your email for confirmation.",
  "data": {
    "token":    "eyJhbGci...",
    "userId":   "abc-123",
    "userName": "johndoe",
    "email":    "john@example.com"
  }
}
```

---

#### `POST /api/auth/login`
```json
{
  "email":    "john@example.com",
  "password": "Secret@123"
}
```

```json
{
  "status":  200,
  "success": true,
  "message": "Login successful.",
  "data": {
    "token":        "eyJhbGci...",
    "refreshToken": "abc123...",
    "expiration":   "2026-03-09T14:00:00Z",
    "userId":       "abc-123",
    "userName":     "johndoe",
    "email":        "john@example.com",
    "roles":        ["User"]
  }
}
```

---

#### `POST /api/auth/logout`
Requires `Authorization: Bearer {token}`.
- Extracts the `jti` claim from the access token
- Writes `blacklist:jti:{jti}` to Redis with TTL = remaining token lifetime
- Revokes the refresh token server-side
- Reusing the access token after logout returns `401 Token has been revoked`

---

#### `POST /api/auth/forgot-password`
```json
{ "email": "john@example.com" }
```
Always returns `200` — never reveals whether the email exists.

---

#### `POST /api/auth/reset-password`
```json
{
  "userId":          "abc-123",
  "token":           "reset_token_from_email",
  "password":        "NewSecret@456",
  "confirmPassword": "NewSecret@456"
}
```

---

### User: `api/user` *(Bearer required)*

| Method | Route | Description |
|---|---|---|
| `GET` | `/me` | Get own profile |
| `PUT` | `/me` | Update profile fields |
| `PUT` | `/me/change-password` | Change password, forces re-login |

#### `PUT /api/user/me`
All fields optional — only provided fields are updated:
```json
{
  "firstName":   "Jane",
  "lastName":    "Doe",
  "phoneNumber": "+1234567890",
  "address":     "123 Main St",
  "profileURL":  "https://example.com/avatar.png",
  "birthDate":   "1995-06-15"
}
```

#### `PUT /api/user/me/change-password`
```json
{
  "currentPassword": "Secret@123",
  "newPassword":     "NewSecret@456",
  "confirmPassword": "NewSecret@456"
}
```

---

### Admin: `api/admin` *(Admin role required)*

| Method | Route | Description |
|---|---|---|
| `GET` | `/users?pageNumber=1&pageSize=10` | Paginated user list (max 50/page) |
| `GET` | `/users/{userId}` | Get user by ID |
| `POST` | `/users/{userId}/promote` | Add Admin role |
| `POST` | `/users/{userId}/demote` | Remove Admin role |
| `POST` | `/users/{userId}/activate` | Re-enable account |
| `POST` | `/users/{userId}/deactivate` | Block login + revoke tokens |
| `DELETE` | `/users/{userId}` | Permanently delete user |

Pagination metadata is returned in the `X-Pagination` response header:
```json
{
  "currentPage": 1,
  "totalPages":  3,
  "pageSize":    10,
  "totalCount":  21,
  "hasPrevious": false,
  "hasNext":     true
}
```

---

## Response Format

Every endpoint returns the same envelope:

```json
{
  "status":  200,
  "success": true,
  "message": "...",
  "data":    { },
  "errors":  ["..."],
  "validationErrors": {
    "fieldName": ["error message"]
  }
}
```

`errors` and `validationErrors` are omitted when empty. All `401` and `429` responses also return this format — never a blank body.

---

## Health Checks

`GET /health` — no authentication required.

Returns `200 OK` when all dependencies are healthy, `503 Service Unavailable` when any check fails.

```json
{
  "status": "Healthy",
  "totalDuration": "00:00:00.0500",
  "entries": {
    "npgsql": {
      "status":   "Healthy",
      "duration": "00:00:00.0120"
    },
    "smtp": {
      "status":      "Healthy",
      "description": "SMTP reachable at smtp.gmail.com:587.",
      "duration":    "00:00:00.0380"
    }
  }
}
```

| Check | What It Verifies |
|---|---|
| `npgsql` | Opens a real connection to PostgreSQL and runs a ping query |
| `smtp` | Opens a TCP connection to the configured SMTP host and port |

---

## Rate Limiting

| Endpoint | Limit | Window |
|---|---|---|
| `POST /login` | 5 requests | per IP / per minute |
| `POST /register` | 3 requests | per IP / per 5 min |
| `POST /forgot-password` | 3 requests | per IP / per 15 min |
| All other endpoints | 60 requests | per IP / per minute |

The rate limiter reads `X-Forwarded-For` first so it correctly identifies real client IPs when running behind a reverse proxy (Nginx, Cloudflare, etc.).

---

## Security Headers

| Header | Value | Protects Against |
|---|---|---|
| `X-Content-Type-Options` | `nosniff` | MIME-type confusion attacks |
| `X-Frame-Options` | `DENY` | Clickjacking via iframes |
| `X-XSS-Protection` | `1; mode=block` | XSS in legacy browsers |
| `Referrer-Policy` | `strict-origin-when-cross-origin` | Referrer info leakage |
| `Permissions-Policy` | `camera=(), microphone=(), geolocation=(), payment=()` | Unwanted browser feature access |
| `Strict-Transport-Security` | `max-age=31536000; includeSubDomains` | SSL stripping, HTTPS downgrade attacks |
| `Content-Security-Policy` | `default-src 'none'; frame-ancestors 'none'` | XSS, data injection, framing |

> `Strict-Transport-Security` is only sent over HTTPS and is intentionally omitted on plain HTTP to avoid breaking local development.
>
> The `Content-Security-Policy` is relaxed automatically for `/swagger` routes in development to allow Swagger UI assets to load correctly.

---

## Structured Logging

Serilog replaces the default .NET logger. Configuration lives in `appsettings.json` under the `Serilog` section — no code changes needed to adjust log levels or add sinks.

Every HTTP request is logged with method, path, status code, duration, client IP, and user agent. Swagger traffic (`/swagger/*`) is logged at `Verbose` level and suppressed by default to keep logs clean.

---

## Strongly-Typed Configs

All environment variables are bound to typed classes in `Configs/` and validated at startup using Data Annotations + `.ValidateOnStart()`. The app **will not start** if any required variable is missing or invalid.

| Class | Env Prefix | Key Variables |
|---|---|---|
| `JwtConfigs` | `JWT__` | `SecretKey`, `ValidIssuer`, `ValidAudience`, expiry |
| `SmtpConfigs` | `Smtp__` | `Host`, `Port`, `Username`, `Password`, `EnableSsl` |
| `SeedConfigs` | `Seed__Admin__` | Admin account credentials |
| `AppConfigs` | `App__` | `BaseUrl` |
| `RedisConfigs` | `Redis__` | `ConnectionString` |

To inject Configs into a service:
```csharp
public class EmailService(IOptions<SmtpConfigs> smtpOptions)
{
    private readonly SmtpConfigs _smtp = smtpOptions.Value;
}
```

> **Docker note**: ASP.NET Core automatically converts `__` to `:` in environment variable names. In `docker-compose.yml` set `Redis__ConnectionString: "redis:6379"` — the app reads it as `Redis:ConnectionString`. In `.env` (local), use `Redis__ConnectionString=localhost:6379` directly.

---

## Email Templates

All templates live in `Templates/Email/` and use `{{Placeholder}}` syntax.

| Template | Trigger | Placeholders |
|---|---|---|
| `ConfirmEmail.html` | On register | `{{FirstName}}`, `{{ConfirmUrl}}`, `{{Year}}` |
| `WelcomeEmail.html` | After email confirmed | `{{FirstName}}`, `{{UserName}}`, `{{Email}}`, `{{Role}}`, `{{LoginUrl}}`, `{{Year}}` |
| `ResetPassword.html` | On forgot-password | `{{FirstName}}`, `{{ResetUrl}}`, `{{Year}}` |

---

## Security

| Concern | Approach |
|---|---|
| Secrets | `.env` via DotNetEnv (Development only), gitignored; validated at startup |
| Passwords | PBKDF2 + salt (ASP.NET Identity) |
| Access token | JWT HS256 · 1 hr · `ClockSkew = 0` |
| Token blacklist | Redis `EXISTS` check on every request · O(1) lookup · auto TTL expiry |
| Refresh token | 64 random bytes · 7 days · rotated on every use |
| Rate limiting | Per-IP fixed-window on auth endpoints; `429` + `Retry-After` |
| Security headers | `HSTS`, `CSP`, `X-Frame-Options`, `X-Content-Type-Options`, `Referrer-Policy`, `Permissions-Policy` on every response |
| HTTPS | Enforced in non-development environments |
| Proxy support | `X-Forwarded-For` / `X-Forwarded-Proto` via `ForwardedHeaders` middleware |
| User enumeration | Login and forgot-password always return the same message |
| Email confirmation | Required before login is allowed |
| Account lockout | 5 failed attempts → 15-minute lockout |
| Password policy | Min 8 chars, uppercase, lowercase, digit, special character |
| Password change | Revokes all refresh tokens → forces re-login |
| Password reset | Decodes URL-encoded token · revokes all refresh tokens |
| Account deactivation | Revokes tokens immediately, blocks all future logins |
| Stack trace | Only exposed in `Development` environment |

---

## Stack

| | |
|---|---|
| Framework | ASP.NET Core 8 |
| ORM | Entity Framework Core 8 |
| Database | PostgreSQL via Npgsql |
| Cache / Blacklist | Redis 7 via StackExchange.Redis |
| Identity | ASP.NET Core Identity |
| Logging | Serilog |
| Containerization | Docker + Docker Compose |
| Secrets | DotNetEnv 3.1 |
| Docs | Swashbuckle / Swagger 6.5 |