# 🔐 AuthCore.API

[![.NET](https://img.shields.io/badge/.NET-8.0-purple)](https://dotnet.microsoft.com)
[![PostgreSQL](https://img.shields.io/badge/PostgreSQL-14+-336791?logo=postgresql&logoColor=white)](https://www.postgresql.org)
[![JWT](https://img.shields.io/badge/Auth-JWT-orange)](https://jwt.io)

A production-ready authentication REST API built with **ASP.NET Core 8** and **PostgreSQL**. Handles the full auth lifecycle — registration, email confirmation, login, JWT + refresh token rotation, token blacklisting on logout, password management, user profiles, and admin controls.

---

## Features

- **JWT Authentication** — short-lived access tokens (1 hour), signed with HS256
- **Refresh Token Rotation** — cryptographically random 64-byte tokens, rotated on every use, expire after 7 days
- **Token Blacklist** — revoked access tokens are stored in DB and rejected on every request via `OnTokenValidated`; re-using a token after logout returns `401`
- **Rate Limiting** — per-IP fixed-window limits on all auth endpoints; consistent `429` JSON response with `Retry-After` header
- **Security Headers** — every response includes `HSTS`, `CSP`, `X-Frame-Options`, `X-Content-Type-Options`, `Referrer-Policy`, and `Permissions-Policy` via a dedicated middleware
- **Email Confirmation** — required before login; sends branded HTML email on register
- **Welcome Email** — sent automatically after email is confirmed
- **Forgot / Reset Password** — secure reset flow via email link; revokes all refresh tokens on reset
- **User Profile** — get and update own profile, change password
- **Role-Based Authorization** — `Admin` and `User` roles seeded automatically on every startup
- **Admin Panel** — paginated user list, promote/demote, activate/deactivate, delete
- **Global Exception Handling** — middleware maps every exception type to a consistent JSON response
- **Consistent 401 Response Body** — unauthorized requests always return `ApiResponse<T>` JSON, never a blank response
- **Strongly-Typed Configs** — all `.env` variables are bound to validated Configs classes; app refuses to start if any required value is missing
- **Environment Secrets** — all secrets in `.env` via `DotNetEnv`, never committed to git
- **HTML Email Templates** — dark-themed, table-based templates for all transactional emails
- **Database Seeding** — admin account seeded on every startup; 20 test users seeded via data migration
- **Swagger UI** — interactive docs with Bearer token support at `/swagger`

---

## Project Structure

```
AuthCore.API/
├── Controllers/
│   ├── AuthController.cs              # Register, Login, Logout, Confirm, ForgotPassword, ResetPassword
│   ├── UserController.cs              # GetProfile, UpdateProfile, ChangePassword
│   └── AdminController.cs             # GetAllUsers, GetUser, Promote, Demote, Activate, Deactivate, Delete
│
├── Data/
│   ├── ApplicationDbContext.cs
│   ├── DbSeeder.cs                    # Admin seeder — runs on every startup (accepts SeedConfigs)
│   └── Migrations/
│       └── SeedTestUsers.cs           # 20 test users — run once via dotnet ef database update
│
├── DTOs/
│   ├── Auth/
│   │   ├── AuthResponseDto.cs
│   │   ├── ConfirmEmailDto.cs
│   │   ├── ForgotPasswordDto.cs
│   |   ├── LoginDto.cs
│   │   ├── RefreshTokenDto.cs
│   |   ├── RegisterDto.cs
│   │   └── ResetPasswordDto.cs
│   ├── User/
│   │   ├── UserDto.cs
│   │   ├── ChangePasswordDto.cs
│   │   └── UpdateProfileDto.cs
│   ├── Email/
│   |   └── ConfirmEmailDto.cs
│   └── DataValidation.cs
│
├── Exceptions/
│   ├── ApiException.cs                # Abstract base
│   ├── BadRequestException.cs         # 400
│   ├── ConflictException.cs           # 409
│   ├── ForbiddenException.cs          # 403
│   ├── NotFoundException.cs           # 404
│   ├── UnauthorizedException.cs       # 401
│   └── ValidationException.cs         # 400 + field errors
│
├── Middleware/
│   ├── ExceptionHandlingMiddleware.cs # Handle exceptions and returns api response errors
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
├── Configs/                           # Strongly-typed configuration classes
│   ├── AppConfigs.cs                  # App__BaseUrl
│   ├── JwtConfigs.cs                  # JWT__SecretKey, Issuer, Audience, expiry
│   ├── SmtpConfigs.cs                 # Smtp__Host, Port, credentials, SSL
│   └── SeedConfigs.cs                 # Seed__Admin__* values
│
├── Templates/
│   └── Email/
│       ├── ConfirmEmail.html          # Sent on register
│       ├── ResetPassword.html         # Sent on forgot-password
│       └── WelcomeEmail.html          # Sent after email confirmed
│
├── .env                               # ⚠️ Secrets — gitignored
├── .env.example                       # ✅ Template — safe to commit
├── AuthCore.API.csproj
└── Program.cs
```

---

## Getting Started

### Prerequisites

- [.NET 8 SDK](https://dotnet.microsoft.com/download)
- [PostgreSQL 14+](https://www.postgresql.org/download/)
- EF Core CLI — `dotnet tool install --global dotnet-ef`

### 1 — Clone & restore

```bash
git clone https://github.com/abdelrahman-kamel-elgendy/AuthCore.API.git
cd AuthCore.API
dotnet restore
```

### 2 — Configure `.env`

```bash
cp .env.example .env
```

Fill in your values:

```env
# Database
ConnectionStrings__PostgreSQL=Host=localhost;Database=AuthCoreDB;Username=postgres;Password=YOUR_PASSWORD

# JWT
JWT__ValidIssuer=http://localhost:5000
JWT__ValidAudience=http://localhost:4200
JWT__SecretKey=AT_LEAST_32_CHARS_LONG_RANDOM_SECRET!@#$%
JWT__AccessTokenExpiryMinutes=60
JWT__RefreshTokenExpiryDays=7

# App
App__BaseUrl=http://localhost:5000

# SMTP
Smtp__Host=smtp.gmail.com
Smtp__Port=587
Smtp__Username=your@gmail.com
Smtp__Password=your_app_password
Smtp__FromName=AuthCore
Smtp__EnableSsl=true

# Seed
Seed__Admin__Email=admin@authcore.com
Seed__Admin__Password=Admin@123456
Seed__Admin__FirstName=Super
Seed__Admin__LastName=Admin
Seed__Admin__UserName=superadmin
```

> `.env` is gitignored and will never be committed. All variables are validated at startup — the app will refuse to start with a clear error if any required value is missing or invalid.
>
> In production, set these as real environment variables on your server or container instead of using a `.env` file.

### 3 — Generate password hash for test users

Before running migrations, generate the hash for `"User@123456"` by adding this line temporarily to `Program.cs` before `app.Run()`:

```csharp
Console.WriteLine(new PasswordHasher<UserModel>().HashPassword(new UserModel(), "User@123456"));
```

Run `dotnet run`, copy the output hash, paste it into `Data/Migrations/SeedTestUsers.cs`:

```csharp
private const string PasswordHash = "paste_your_hash_here";
```

Then remove the temporary line.

### 4 — Migrate & run

```bash
dotnet ef database update
dotnet run
```

Open **http://localhost:5000/swagger** 🎉

---

## API Reference

### Auth — `api/auth`

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
  "firstName": "John",
  "lastName":  "Doe",
  "username":  "johndoe",
  "email":     "john@example.com",
  "password":        "Secret@123",
  "confirmPassword": "Secret@123"
}
```
Optional: `phoneNumber`, `address`, `birthDate`, `profileURL`.

---

#### `POST /api/auth/login`
```json
{ "email": "john@example.com", "password": "Secret@123" }
```
```json
{
  "success": true,
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

- Blacklists the access token's `jti` in the `RevokedTokens` table
- Revokes the refresh token server-side
- Calling logout again with the same token returns `401`

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
  "newPassword":     "NewSecret@456",
  "confirmPassword": "NewSecret@456"
}
```

---

### User — `api/user` *(Bearer required)*

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

### Admin — `api/admin` *(Admin role required)*

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

Every response includes a set of HTTP security headers injected by `SecurityHeadersMiddleware`. No client configuration is required — they are applied automatically at the middleware level.

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

## Token Blacklist

On logout, the JWT's `jti` claim and expiry are stored in the `RevokedTokens` table. Every subsequent authenticated request checks this table via `OnTokenValidated` in the JWT middleware. If the `jti` is found, the request is rejected with `401` before reaching the controller. Expired entries can be purged anytime via `TokenBlacklistService.PurgeExpiredAsync()`.

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
| Token blacklist | `jti` stored in DB on logout, checked on every request |
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
| Secrets | DotNetEnv 3.1 |
| Docs | Swashbuckle / Swagger 6.5 |