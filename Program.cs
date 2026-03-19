using AuthCore.API.Configs;
using AuthCore.API.Data;
using AuthCore.API.Exceptions;
using AuthCore.API.HealthChecks;
using AuthCore.API.Middleware;
using AuthCore.API.Models;
using AuthCore.API.Repositories;
using AuthCore.API.Services;
using AuthCore.API.Services.Interfaces;
using HealthChecks.UI.Client;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using StackExchange.Redis;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Threading.RateLimiting;
using Karambolo.Extensions.Logging.File;
using Asp.Versioning;

var builder = WebApplication.CreateBuilder(args);

// == Environment & Configuration ===============================================
if (builder.Environment.IsDevelopment())
    DotNetEnv.Env.Load();

builder.Configuration.AddEnvironmentVariables();

// == Logging ===================================================================
builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddFile(options =>
{
    options.RootPath = builder.Environment.ContentRootPath;
    options.BasePath = "Logs";
    options.FileEncodingName = "utf-8";
    options.MaxFileSize = 10 * 1024 * 1024; // 10 MB
    options.Files = new[]
    {
        new LogFileOptions { Path = "authcore-<date>.log" }
    };
});

if (builder.Environment.IsDevelopment())
    builder.Logging.AddDebug();

// == Strongly-Typed Configs ====================================================
builder.Services
    .AddOptions<JwtConfigs>()
    .BindConfiguration(JwtConfigs.SectionName)
    .ValidateDataAnnotations()
    .ValidateOnStart();

builder.Services
    .AddOptions<SmtpConfigs>()
    .BindConfiguration(SmtpConfigs.SectionName)
    .ValidateDataAnnotations()
    .ValidateOnStart();

builder.Services
    .AddOptions<SeedConfigs>()
    .BindConfiguration(SeedConfigs.SectionName)
    .ValidateDataAnnotations()
    .ValidateOnStart();

builder.Services
    .AddOptions<AppConfigs>()
    .BindConfiguration(AppConfigs.SectionName)
    .ValidateDataAnnotations()
    .ValidateOnStart();

builder.Services
    .AddOptions<RateLimitConfigs>()
    .BindConfiguration(RateLimitConfigs.SectionName)
    .ValidateDataAnnotations()
    .ValidateOnStart();

// == Forwarded Headers =========================================================
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    options.KnownNetworks.Clear();
    options.KnownProxies.Clear();
});

// == Rate Limiting =============================================================
var rateLimitConfigs = builder.Configuration
    .GetSection(RateLimitConfigs.SectionName)
    .Get<RateLimitConfigs>() ?? new RateLimitConfigs();

builder.Services.AddRateLimiter(options =>
{
    static string GetClientIp(HttpContext ctx) =>
        ctx.Request.Headers["X-Forwarded-For"].FirstOrDefault()?.Split(',')[0].Trim()
        ?? ctx.Connection.RemoteIpAddress?.ToString()
        ?? "unknown";

    options.AddPolicy("login", ctx =>
        RateLimitPartition.GetFixedWindowLimiter(GetClientIp(ctx),
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = rateLimitConfigs.Login.PermitLimit,
                Window = TimeSpan.FromMinutes(rateLimitConfigs.Login.WindowMinutes),
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                QueueLimit = 0
            }));

    options.AddPolicy("register", ctx =>
        RateLimitPartition.GetFixedWindowLimiter(GetClientIp(ctx),
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = rateLimitConfigs.Register.PermitLimit,
                Window = TimeSpan.FromMinutes(rateLimitConfigs.Register.WindowMinutes),
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                QueueLimit = 0
            }));

    options.AddPolicy("forgot-password", ctx =>
        RateLimitPartition.GetFixedWindowLimiter(GetClientIp(ctx),
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = rateLimitConfigs.ForgotPassword.PermitLimit,
                Window = TimeSpan.FromMinutes(rateLimitConfigs.ForgotPassword.WindowMinutes),
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                QueueLimit = 0
            }));

    options.AddPolicy("global", ctx =>
        RateLimitPartition.GetFixedWindowLimiter(GetClientIp(ctx),
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = rateLimitConfigs.Global.PermitLimit,
                Window = TimeSpan.FromMinutes(rateLimitConfigs.Global.WindowMinutes),
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                QueueLimit = 0
            }));

    options.OnRejected = async (context, cancellationToken) =>
    {
        var retryAfter = 0;
        if (context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfterSpan))
            retryAfter = (int)retryAfterSpan.TotalSeconds;

        throw new TooManyRequestsException("Too many requests. Please slow down and try again later.", retryAfter);
    };
});

// == Controllers ===============================================================
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

builder.Services.AddApiVersioning(options =>
{
    options.DefaultApiVersion = new ApiVersion(1, 0);
    options.AssumeDefaultVersionWhenUnspecified = true;
    options.ReportApiVersions = true;
})
.AddApiExplorer(options =>
{
    options.GroupNameFormat = "'v'VVV";
    options.SubstituteApiVersionInUrl = true;
});

// == Swagger ===================================================================
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "AuthCore API",
        Version = "v1",
        Description = "Authentication REST API built with ASP.NET Core 8 and PostgreSQL",
        Contact = new OpenApiContact
        {
            Name = "Abdelrahman Kamel",
            Email = "abdelrahman.kamel.elgendy@gmail.com",
            Url = new Uri("https://github.com/abdelrahman-kamel-elgendy")
        }
    });

    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "Enter 'Bearer {your_token}'",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.Http,
        Scheme = "Bearer",
        BearerFormat = "JWT"
    });

    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
            },
            Array.Empty<string>()
        }
    });
});

// == Database ==================================================================
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("PostgreSQL")));

// == Identity ==================================================================
builder.Services.AddIdentity<UserModel, IdentityRole>(options =>
{
    options.Password.RequireDigit = true;
    options.Password.RequiredLength = 8;
    options.Password.RequireNonAlphanumeric = true;
    options.Password.RequireUppercase = true;
    options.Password.RequireLowercase = true;

    options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
    options.Lockout.MaxFailedAccessAttempts = 5;
    options.Lockout.AllowedForNewUsers = true;

    options.User.RequireUniqueEmail = true;
    options.SignIn.RequireConfirmedEmail = true;
})
.AddEntityFrameworkStores<ApplicationDbContext>()
.AddDefaultTokenProviders();

builder.Services.Configure<DataProtectionTokenProviderOptions>(opt =>
    opt.TokenLifespan = TimeSpan.FromHours(2));

// == JWT Authentication ========================================================
var jwtConfigs = builder.Configuration
    .GetSection(JwtConfigs.SectionName)
    .Get<JwtConfigs>()
    ?? throw new InvalidOperationException("JwtConfigs could not be loaded.");

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.RequireHttpsMetadata = !builder.Environment.IsDevelopment();
    options.SaveToken = true;

    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtConfigs.SecretKey)),
        ValidateIssuer = true,
        ValidIssuer = jwtConfigs.ValidIssuer,
        ValidateAudience = true,
        ValidAudience = jwtConfigs.ValidAudience,
        ValidateLifetime = true,
        ClockSkew = TimeSpan.Zero
    };

    options.Events = new JwtBearerEvents
    {
        OnChallenge = context =>
        {
            context.HandleResponse();

            var message = context.AuthenticateFailure?.Message
                       ?? "You are not authorized to access this resource.";

            throw new UnauthorizedException(message);
        },

        OnTokenValidated = async context =>
        {
            var jti = context.Principal?.FindFirstValue(JwtRegisteredClaimNames.Jti);

            if (string.IsNullOrEmpty(jti))
                throw new UnauthorizedException("Token is missing the jti claim.");

            var blacklist = context.HttpContext.RequestServices
                .GetRequiredService<ITokenBlacklistService>();

            if (await blacklist.IsRevokedAsync(jti))
                throw new UnauthorizedException("Token has been revoked.");
        }
    };
});

// == Repositories & Services ===================================================
builder.Services.AddScoped<IAuthRepository, AuthRepository>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IAdminService, AdminService>();
builder.Services.AddScoped<IEmailService, EmailService>();

// == Redis =====================================================================
var redisConnection = builder.Configuration["Redis:ConnectionString"]
    ?? throw new InvalidOperationException("Redis connection string is not configured.");

builder.Services.AddSingleton<IConnectionMultiplexer>(
    ConnectionMultiplexer.Connect(redisConnection));

builder.Services.AddScoped<ITokenBlacklistService, TokenBlacklistService>();

// == Health Checks =============================================================
builder.Services.AddHealthChecks()
    .AddNpgSql(builder.Configuration.GetConnectionString("PostgreSQL")!)
    .AddCheck<SmtpHealthCheck>("smtp");

// == Build =====================================================================
var app = builder.Build();

// ── Middleware pipeline (order is significant) ─────────────────────────────────

// 1. Global exception handler — must be outermost to catch everything
app.UseMiddleware<ExceptionHandlingMiddleware>();

// 2. Security headers — applied to every response
app.UseMiddleware<SecurityHeadersMiddleware>();

// 3. Forwarded headers — resolve real IP before rate limiter reads it
app.UseForwardedHeaders();

// 4. Request logging — after forwarded headers so ClientIP is already resolved
app.UseMiddleware<RequestLoggingMiddleware>();

// 5. Rate limiting — before auth so all requests are covered
app.UseRateLimiter();

// 6. HTTPS redirect — skipped in development
if (!app.Environment.IsDevelopment())
    app.UseHttpsRedirection();

// 7. Swagger — development only
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "AuthCore API v1"));
}

// 8. Auth
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapHealthChecks("/health", new HealthCheckOptions
{
    ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
}).AllowAnonymous();

// == Migrate DB & Seed on startup ==============================================
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

    await DbSeeder.SeedAsync(
        scope.ServiceProvider.GetRequiredService<UserManager<UserModel>>(),
        db,
        scope.ServiceProvider.GetRequiredService<IOptions<SeedConfigs>>().Value,
        scope.ServiceProvider.GetRequiredService<ILogger<Program>>());
}

app.Run();