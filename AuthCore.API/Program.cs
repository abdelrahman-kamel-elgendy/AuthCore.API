using AuthCore.API.Data;
using AuthCore.API.Middleware;
using AuthCore.API.Models;
using AuthCore.API.Repositories;
using AuthCore.API.Services;
using AuthCore.API.Services.Interfaces;
using AuthCore.API.Configs;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Net;
using System.Text;
using System.Threading.RateLimiting;

var builder = WebApplication.CreateBuilder(args);

// == Environment & Configuration ===============================================
// Load .env into environment variables first — before anything reads config.
// In production, set real environment variables instead of using a .env file.
DotNetEnv.Env.Load();
builder.Configuration.AddEnvironmentVariables();

// == Strongly-Typed Configs ===================================================
// Bind every .env section to a typed class and validate required fields on
// startup — if anything is missing, the app refuses to start with a clear error.
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

// == Forwarded Headers =========================================================
// Required when running behind a reverse proxy (Nginx, Cloudflare, etc.)
// so RemoteIpAddress reflects the real client IP, not the proxy IP.
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    // In production, restrict to known proxy IPs to prevent header spoofing:
    //   options.KnownProxies.Add(IPAddress.Parse("10.0.0.1"));
    options.KnownNetworks.Clear();
    options.KnownProxies.Clear();
});

// == Rate Limiting =============================================================
builder.Services.AddRateLimiter(options =>
{
    // Resolves the real client IP — works behind proxies too
    static string GetClientIp(HttpContext ctx) =>
        ctx.Request.Headers["X-Forwarded-For"].FirstOrDefault()?.Split(',')[0].Trim()
        ?? ctx.Connection.RemoteIpAddress?.ToString()
        ?? "unknown";

    // 1. Login: 5 attempts / minute / IP
    options.AddPolicy("login", ctx =>
        RateLimitPartition.GetFixedWindowLimiter(GetClientIp(ctx),
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 5,
                Window = TimeSpan.FromMinutes(1),
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                QueueLimit = 0
            }));

    // 2. Register: 3 attempts / 5 minutes / IP
    options.AddPolicy("register", ctx =>
        RateLimitPartition.GetFixedWindowLimiter(GetClientIp(ctx),
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 3,
                Window = TimeSpan.FromMinutes(5),
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                QueueLimit = 0
            }));

    // 3. Forgot Password: 3 attempts / 15 minutes / IP
    options.AddPolicy("forgot-password", ctx =>
        RateLimitPartition.GetFixedWindowLimiter(GetClientIp(ctx),
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 3,
                Window = TimeSpan.FromMinutes(15),
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                QueueLimit = 0
            }));

    // 4. Global fallback: 60 requests / minute / IP
    options.AddPolicy("global", ctx =>
        RateLimitPartition.GetFixedWindowLimiter(GetClientIp(ctx),
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 60,
                Window = TimeSpan.FromMinutes(1),
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                QueueLimit = 0
            }));

    // Consistent 429 JSON body — matches the rest of the API's ApiResponse<T> envelope
    options.OnRejected = async (context, cancellationToken) =>
    {
        context.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;
        context.HttpContext.Response.ContentType = "application/json";

        if (context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter))
            context.HttpContext.Response.Headers.RetryAfter =
                ((int)retryAfter.TotalSeconds).ToString();

        await context.HttpContext.Response.WriteAsJsonAsync(
            new ApiResponse<object>(
                HttpStatusCode.TooManyRequests,
                false,
                "Too many requests. Please slow down and try again later.",
                "Rate limit exceeded."
            ),
            cancellationToken
        );
    };
});

// == Controllers ===============================================================
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

// == Swagger ===================================================================
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "AuthCore API",
        Version = "v1",
        Description = "JWT-based authentication API built with ASP.NET Core 8 and PostgreSQL."
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
// Read JWT config from the already-validated JwtConfigs — no raw string access.
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
        OnChallenge = async context =>
        {
            context.HandleResponse();

            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            context.Response.ContentType = "application/json";

            var message = context.AuthenticateFailure?.Message
                       ?? "You are not authorized to access this resource.";

            await context.Response.WriteAsJsonAsync(
                new ApiResponse<object>(HttpStatusCode.Unauthorized, false, message, null)
            );
        }
    };
});

// == Repositories & Services ===================================================
builder.Services.AddScoped<IAuthRepository, AuthRepository>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IAdminService, AdminService>();
builder.Services.AddScoped<IEmailService, EmailService>();

// == Build =====================================================================
var app = builder.Build();

// ── Middleware pipeline (order is significant) ────────────────────────────────
// 1. Global exception handler — must be outermost to catch everything
app.UseMiddleware<ExceptionHandlingMiddleware>();

// 2. Forwarded headers — must resolve real IP before rate limiter reads it
app.UseForwardedHeaders();

// 3. Rate limiting — applied before auth so all requests are covered
app.UseRateLimiter();

// 4. HTTPS redirect — skipped in development
if (!app.Environment.IsDevelopment())
    app.UseHttpsRedirection();

// 5. Swagger — development only
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "AuthCore API v1"));
}

// 6. Auth
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

// == Migrate DB & Seed on startup ==============================================
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
    var userManager = scope.ServiceProvider.GetRequiredService<UserManager<UserModel>>();

    await db.Database.MigrateAsync();

    foreach (var role in new[] { "Admin", "User" })
        if (!await roleManager.RoleExistsAsync(role))
            await roleManager.CreateAsync(new IdentityRole(role));

    // Pass IOptions<SeedConfigs> instead of raw IConfiguration
    var seedConfigs = scope.ServiceProvider.GetRequiredService<IOptions<SeedConfigs>>();
    await DbSeeder.SeedAsync(userManager, seedConfigs.Value);
}

app.Run();