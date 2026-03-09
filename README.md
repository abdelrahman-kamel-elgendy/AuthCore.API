# 🔐 AuthCore.API

[![.NET](https://img.shields.io/badge/.NET-8.0-purple)](https://dotnet.microsoft.com)
[![PostgreSQL](https://img.shields.io/badge/PostgreSQL-14+-336791?logo=postgresql&logoColor=white)](https://www.postgresql.org)
[![JWT](https://img.shields.io/badge/Auth-JWT-orange)](https://jwt.io)
[![License](https://img.shields.io/badge/license-MIT-green)](LICENSE)

A production-ready authentication REST API built with **ASP.NET Core 8** and **PostgreSQL**. Handles the full auth lifecycle — registration, email confirmation, login, JWT + refresh token rotation, token blacklisting on logout, password management, user profiles, and admin controls.

---

## Features

- **JWT Authentication** — short-lived access tokens (1 hour), signed with HS256
- **Refresh Token Rotation** — cryptographically random 64-byte tokens, rotated on every use, expire after 7 days
- **Token Blacklist** — revoked access tokens are stored in DB and rejected on every request via `OnTokenValidated`; re-using a token after logout returns `401`
- **Email Confirmation** — required before login; sends branded HTML email on register
- **Welcome Email** — sent automatically after email is confirmed
- **Forgot / Reset Password** — secure reset flow via email link; revokes all refresh tokens on reset
- **User Profile** — get and update own profile, change password
- **Role-Based Authorization** — `Admin` and `User` roles seeded automatically on every startup
- **Admin Panel** — paginated user list, promote/demote, activate/deactivate, delete
- **Global Exception Handling** — middleware maps every exception type to a consistent JSON response
- **Consistent 401 Response Body** — unauthorized requests always return `ApiResponse<T>` JSON, never a blank response
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
│   ├── DbSeeder.cs                    # Admin seeder — runs on every startup
│   └── Migrations/
│       └── SeedTestUsers.cs           # 20 test users — run once via dotnet ef database update
│
├── DTOs/
│   ├── Auth/
│   │   ├── AuthResponseDto.cs
│   │   ├── ConfirmEmailDto.cs
│   │   ├── ForgotPasswordDto.cs
│   │   ├── RefreshTokenDto.cs
│   │   └── ResetPasswordDto.cs
│   ├── User/
│   │   ├── ChangePasswordDto.cs
│   │   └── UpdateProfileDto.cs
│   ├── LoginDto.cs
│   ├── RegisterDto.cs
│   └── UserDto.cs
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
│   └── ExceptionHandlingMiddleware.cs
│
├── Models/
│   ├── ApiResponse.cs
│   ├── PagedList.cs
│   ├── PaginationMetadata.cs
│   ├── RevokedToken.cs                # Token blacklist entry
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
│   │   ├── ITokenBlacklistService.cs
│   │   └── IUserService.cs
│   ├── AdminService.cs
│   ├── AuthService.cs
│   ├── EmailService.cs
│   ├── EmailTemplateService.cs
│   └── TokenBlacklistService.cs       # Stores/checks revoked JWT jti claims
│
├── Templates/
│   └── Email/
│       ├── ConfirmEmail.html          # Sent on register
│       ├── ResetPassword.html         # Sent on forgot-password
│       └── WelcomeEmail.html          # Sent after email confirmed
│
├── .env                               # ⚠️ Secrets — gitignored
├── .env.example                       # ✅ Template — safe to commit
├── appsettings.json
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
ConnectionStrings__PostgreSQL=Host=localhost;Database=AuthCoreDB;Username=postgres;Password=YOUR_PASSWORD
JWT__ValidIssuer=http://localhost:5000
JWT__ValidAudience=http://localhost:4200
JWT__SecretKey=AT_LEAST_32_CHARS_LONG_RANDOM_SECRET!@#$%

AppBaseUrl=http://localhost:5000

Smtp__Host=smtp.gmail.com
Smtp__Port=587
Smtp__Username=your@gmail.com
Smtp__Password=your_app_password
Smtp__FromName=AuthCore

Seed__Admin__Email=admin@authcore.com
Seed__Admin__Password=Admin@123456
Seed__Admin__FirstName=Super
Seed__Admin__LastName=Admin
Seed__Admin__UserName=superadmin
```

> `.env` is gitignored and will never be committed. In production, set these as real environment variables on your server or container.

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

| Method | Route | Auth | Description |
|---|---|---|---|
| `POST` | `/register` | — | Register new account, sends confirmation email |
| `GET` | `/confirm-email?userId=&token=` | — | Confirm email via link, sends welcome email |
| `POST` | `/login` | — | Login, returns access + refresh token |
| `POST` | `/refresh-token` | — | Rotate refresh token |
| `POST` | `/logout` | Bearer | Blacklist access token + revoke refresh token |
| `POST` | `/forgot-password` | — | Send password reset link (always returns 200) |
| `POST` | `/reset-password` | — | Reset password, revokes all refresh tokens |

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

`errors` and `validationErrors` are omitted when empty. All `401` responses — missing token, expired token, revoked token — also return this format.

---

## Token Blacklist

On logout, the JWT's `jti` claim and expiry are stored in the `RevokedTokens` table. Every subsequent authenticated request checks this table via `OnTokenValidated` in the JWT middleware. If the `jti` is found, the request is rejected with `401` before reaching the controller. Expired entries can be purged anytime via `TokenBlacklistService.PurgeExpiredAsync()`.

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
| Secrets | `.env` via DotNetEnv, gitignored |
| Passwords | PBKDF2 + salt (ASP.NET Identity) |
| Access token | JWT HS256 · 1 hr · `ClockSkew = 0` |
| Refresh token | 64 random bytes · 7 days · rotated on every use |
| Token blacklist | `jti` stored in DB on logout, checked on every request |
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

