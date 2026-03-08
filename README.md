# 🔐 AuthCore.API

![.NET](https://img.shields.io/badge/.NET-8-purple)
![ASP.NET](https://img.shields.io/badge/ASP.NET-Core-blue)
![License](https://img.shields.io/badge/license-MIT-green)

**AuthCore.API** is a secure authentication API built with ASP.NET Core and .NET 8.  
It provides JWT-based authentication and a scalable architecture for modern backend systems.

---

## 🏗️ Project Structure 
```text
AuthCore.API
│
├── AuthCore.API
|    ├── Configs
|    ├── Controllers
|    ├── Data
|    ├── Middleware
|    ├── Models
|    ├── Repositories
|    ├── Services
|    ├── DTOs
|    ├── Exceptions
|    |   ├──  ApiException.cs
|    |   ├──  BadRequestException.cs
|    |   ├──  ConflictException.cs
|    |   ├──  ForbiddenException.cs
|    |   ├──  NotFoundException.cs
|    |   ├──  UnauthorizationException.cs
|    |   └──  VaidationExceptions.cs
|    └── Middeleware
|        └──  ExceptionHandlingMiddleware.cs
├── .gitignore.cs
├── AuthCore.API.csproj
├── Program.cs
├── appsettings.json
└── appsettings.json
```
---

## 🚀 Technologies

- .NET 8
- ASP.NET Core Web API
- Entity Framework Core
- JWT Authentication
- Swagger / OpenAPI

---

## ⚙️ Installation

` bash
git clone https://github.com/abdelrahman-kamel-elgendy/AuthCore.API.git
cd AuthCore.API
dotnet restore
`

## Generated
2026-03-08
