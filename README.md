<div align=center>

# 🔐 AuthCore.API

[![.NET](https://img.shields.io/badge/.NET-8.0-purple)](https://dotnet.microsoft.com)
[![PostgreSQL](https://img.shields.io/badge/PostgreSQL-14+-336791?logo=postgresql&logoColor=white)](https://www.postgresql.org)
[![JWT](https://img.shields.io/badge/Auth-JWT-orange)](https://jwt.io)

A production-ready authentication REST API built with **ASP.NET Core 8** and **PostgreSQL**. Handles the full auth lifecycle, registration, email confirmation, login, JWT + refresh token rotation on logout, password management, user profiles, and admin controls.

</div>

---

## Features

- **JWT Authentication**: access tokens signed with HS256, expire after 1 hour with zero clock skew tolerance; refresh tokens are cryptographically random 64-byte values, rotated on every use and expired after 7 days
- **Refresh Token Rotation**: every `/refresh-token` call invalidates the old token and issues a new one; reuse of a consumed token is detected and rejected
- **Email Confirmation**: account login is blocked until the email address is verified; confirmation and welcome emails are sent automatically using dark-themed HTML templates
- **Forgot / Reset Password**: time-limited reset links sent via email; successful reset immediately revokes all active refresh tokens across all devices
- **Role-Based Authorization**: `Admin` and `User` roles enforced at the controller level via `[Authorize(Roles = "...")]`; roles and the default admin account are seeded on every startup
- **Admin Controls**: paginated user management with promote/demote, activate/deactivate, and hard delete; deactivation immediately revokes all tokens and blocks future logins
- **Rate Limiting**: per-IP fixed-window limits on sensitive endpoints (`5/min` login, `3/5min` register, `3/15min` forgot-password) using .NET 8's built-in `RateLimiter`; all rejections return a consistent `429` body with a `Retry-After` header
- **Security Headers**: `SecurityHeadersMiddleware` injects `HSTS`, `CSP`, `X-Frame-Options`, `X-Content-Type-Options`, `Referrer-Policy`, and `Permissions-Policy` on every response; CSP is relaxed automatically for Swagger in development
- **Structured Logging**: Serilog replaces the default logger with configurable sinks (Console, File, Seq); every request is logged with method, path, status code, duration, client IP, and user agent via `UseSerilogRequestLogging`; Swagger traffic is silenced at `Verbose` level
- **Health Checks**: `GET /health` reports live status of PostgreSQL and SMTP; returns `200` when all dependencies are healthy, `503` when any fail; response includes per-check status and duration in JSON
- **Strongly-Typed Configuration**: all environment variables are bound to validated settings classes (`JwtConfigs`, `SmtpConfigs`, `SeedConfigs`, `AppConfigs`) with `.ValidateOnStart()`; the app refuses to start if any required value is missing or invalid
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
│   ├── SmtpConfigs.cs                 # Smtp__Host, Port, credentials, SSL
│   └── SeedConfigs.cs                 # Seed__Admin__* values
│
├── Data/
│   ├── ApplicationDbContext.cs
│   └── DbSeeder.cs                    # Admin seeder: runs on every startup (accepts SeedConfigs)
│
├── DTOs/
│   ├── Auth/
│   │   ├── AuthResponseDto.cs
│   │   ├── ConfirmEmailDto.cs
│   │   ├── ForgotPasswordDto.cs
│   │   ├── LoginDto.cs
│   │   ├── RefreshTokenDto.cs
│   │   ├── RegisterDto.cs
│   │   └── ResetPasswordDto.cs
│   ├── User/
│   │   ├── UserDto.cs
│   │   ├── ChangePasswordDto.cs
│   │   └── UpdateProfileDto.cs
│   ├── Email/
│   │   └── ConfirmEmailDto.cs
│   └── DataValidation.cs
│
├── Exceptions/
│   ├── ApiException.cs                # Abstract base
│   └── CustomExceptions.cs            # 400, 401, 403, 404, 409 exception types
│
├── HealthChecks/
│   └── SmtpHealthCheck.cs             # TCP probe — verifies SMTP host:port is reachable
│
├── Middleware/
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
│   │   └── IUserService.cs
│   ├── AdminService.cs
│   ├── AuthService.cs
│   ├── EmailService.cs
│   └── EmailTemplateService.cs
│
├── Templates/
│   └── Email/
│       ├── ConfirmEmail.html          # Sent on register
│       ├── ResetPassword.html         # Sent on forgot-password
│       └── WelcomeEmail.html          # Sent after email confirmed
│
├── .env                               # ⚠️ Secrets: gitignored
├── .env.example                       # ✅ Template: safe to commit
├── appsettings.json                   # Serilog configuration + app settings
├── AuthCore.API.csproj
└── Program.cs
```

---

## Getting Started

### Prerequisites

- [.NET 8 SDK](https://dotnet.microsoft.com/download)
- [PostgreSQL 14+](https://www.postgresql.org/download/)
- EF Core CLI: `dotnet tool install --global dotnet-ef`

### 1. Clone & restore
```bash
git clone https://github.com/abdelrahman-kamel-elgendy/AuthCore.API.git
cd AuthCore.API
dotnet restore
```

### 2. Install EF Core CLI
```bash
dotnet tool install --global dotnet-ef
```

### 3. Configure `.env`
```bash
cp .env.example .env
```

Fill in all values in `.env` — database connection, JWT secret, SMTP credentials, and seed admin details.

> **Gmail SMTP**: you must use an [App Password](https://myaccount.google.com/apppasswords), not your regular Gmail password. Enable 2FA on your account first, then generate an app password under *Security → 2-Step Verification → App passwords*.

### 4. Create the PostgreSQL database
```bash
psql -U postgres -c "CREATE DATABASE AuthCoreDB;"
```

Or create it via pgAdmin if you prefer a GUI.

### 5. Apply migrations
```bash
dotnet ef database update
```

This creates all tables, and prepares the schema.

### 6. Run
```bash
dotnet run
```

Open **http://localhost:5000/swagger** 🎉

> **First login**: use the admin credentials you set in `.env` under `Seed__Admin__*`.

---

## API Reference

### Auth: `api/auth`

| Method | Route | Auth | Rate Limit | Description |
|---|---|---|---|---|
| `POST` | `/register` | — | 3 / 5 min / IP | Register new account, sends confirmation email |
| `GET` | `/confirm-email?userId=&token=` | — | — | Confirm email via link, sends welcome email |
| `POST` | `/login` | — | 5 / 1 min / IP | Login, returns access + refresh token |
| `POST` | `/refresh-token` | — | 60 / 1 min / IP | Rotate refresh token |
| `POST` | `/logout` | Bearer | — | Blacklist access token + revoke refresh token |
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
  "status": 201,
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
Optional: `rememberMe`.

```json
{
  "status": 200,
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
- Revokes the refresh token server-side
- Calling logout again with the same token returns `401`

```json
{
  "status": 200,
  "success": true,
  "message": "Logged out successfully.",
  "data": {
    "userId":    "abc-123",
    "userName":  "johndoe",
    "firstName": "John",
    "email":     "john@example.com"
  }
}
```

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
```json
{
  "status": 200,
  "success": true,
  "message": "Password reset successfully. Please log in with your new password."
}
```

---

### User: `api/user` *(Bearer required)*

| Method | Route | Description |
|---|---|---|
| `GET` | `/me` | Get own profile |
| `PUT` | `/me` | Update profile fields |
| `PUT` | `/me/change-password` | Change password, forces re-login |

#### `GET /api/user/me`
```json
{
  "status": 200,
  "success": true,
  "message": "Profile retrieved successfully.",
  "data": {
    "id":          "abc-123",
    "userName":    "johndoe",
    "email":       "john@example.com",
    "firstName":   "Jane",
    "lastName":    "Doe",
    "phoneNumber": "+1234567890",
    "profileURL":  "https://example.com/avatar.png",
    "address":     "123 Main St",
    "birthDate":   "1995-06-15"
  }
}
```

#### `PUT /api/user/me`
All fields optional — only provided fields are updated:
```json
{
  "firstName":   "Jane",
  "lastName":    "Doe",
  "phoneNumber": "+1234567888",
  "address":     "125 Main St",
  "profileURL":  "https://example.com/profile.png",
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

Reports the live status of all critical dependencies. Returns `200 OK` when everything is healthy, `503 Service Unavailable` when any check fails.

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

Per-IP fixed-window limits protect all sensitive auth endpoints. When a limit is exceeded, the API returns `429 Too Many Requests` with a `Retry-After` header indicating how long to wait.

| Endpoint | Limit | Window |
|---|---|---|
| `POST /login` | 5 requests | per IP / per minute |
| `POST /register` | 3 requests | per IP / per 5 min |
| `POST /forgot-password` | 3 requests | per IP / per 15 min |
| All other endpoints | 60 requests | per IP / per minute |

The rate limiter reads `X-Forwarded-For` first so it correctly identifies real client IPs when running behind a reverse proxy (Nginx, Cloudflare, etc.).

---

## Security Headers

Every response includes a set of HTTP security headers injected by `SecurityHeadersMiddleware`.

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

Every HTTP request is logged with:

```
HTTP GET /api/auth/login responded 200 in 12.3ms
  RequestHost:   localhost:5000
  RequestScheme: http
  UserAgent:     Mozilla/5.0 ...
  ClientIP:      192.168.1.1
```

Swagger traffic (`/swagger/*`) is logged at `Verbose` level and suppressed by default to keep logs clean.

---

## Strongly-Typed Configs

All environment variables are bound to typed classes in `Configs/` and validated at startup using Data Annotations + `.ValidateOnStart()`. The app **will not start** if any required variable is missing or invalid.

| Class | Env Prefix | Key Variables |
|---|---|---|
| `JwtConfigs` | `JWT__` | `SecretKey`, `ValidIssuer`, `ValidAudience`, expiry |
| `SmtpConfigs` | `Smtp__` | `Host`, `Port`, `Username`, `Password`, `EnableSsl` |
| `SeedConfigs` | `Seed__Admin__` | Admin account credentials |
| `AppConfigs` | `App__` | `BaseUrl` |

To inject Configs into a service:
```csharp
public class EmailService(IOptions<SmtpConfigs> smtpOptions)
{
    private readonly SmtpConfigs _smtp = smtpOptions.Value;
}
```

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
| Secrets | `.env` via DotNetEnv, gitignored; validated at startup |
| Passwords | PBKDF2 + salt (ASP.NET Identity) |
| Access token | JWT HS256 · 1 hr · `ClockSkew = 0` |
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
| Identity | ASP.NET Core Identity |
| Logging | Serilog |
| Secrets | DotNetEnv 3.1 |
| Docs | Swashbuckle / Swagger 6.5 |