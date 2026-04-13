using System.Text;
using System.Threading.RateLimiting;
using AuthCore.API.Configs;
using AuthCore.API.Data;
using AuthCore.API.HealthChecks;
using AuthCore.API.Middleware;
using AuthCore.API.Models;
using AuthCore.API.Repositories;
using AuthCore.API.Services;
using AuthCore.API.Services.Interfaces;
using DotNetEnv;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Serilog;
using Serilog.Events;

try
{
    Env.Load();
    var builder = WebApplication.CreateBuilder(args);
    builder.Configuration.AddEnvironmentVariables();

    // Configure Serilog
    Log.Logger = new LoggerConfiguration()
        .MinimumLevel.Information()
        .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
        .MinimumLevel.Override("Microsoft.AspNetCore", LogEventLevel.Information)
        .MinimumLevel.Override("Microsoft.EntityFrameworkCore", LogEventLevel.Warning)
        .Enrich.FromLogContext()
        .Enrich.WithProperty("Application", "AuthCore.API")
        .WriteTo.Console(outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}")
        .WriteTo.File("logs/authcore-.log", rollingInterval: RollingInterval.Day)
        .CreateLogger();

    builder.Host.UseSerilog();

    // Load configuration
    builder.Configuration
        .SetBasePath(Directory.GetCurrentDirectory())
        .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
        .AddJsonFile($"appsettings.{builder.Environment.EnvironmentName}.json", optional: true)
        .AddEnvironmentVariables();

    // == Strongly-Typed Configs ====================================================
    builder.Services.Configure<JwtConfigs>(builder.Configuration.GetSection(JwtConfigs.SectionName));
    builder.Services.Configure<SmtpConfigs>(builder.Configuration.GetSection(SmtpConfigs.SectionName));
    builder.Services.Configure<AppConfigs>(builder.Configuration.GetSection(AppConfigs.SectionName));
    builder.Services.Configure<SeedConfigs>(builder.Configuration.GetSection(SeedConfigs.SectionName));

    // Validate settings
    var jwtConfigs = builder.Configuration.GetSection(JwtConfigs.SectionName).Get<JwtConfigs>();
    var smtpConfigs = builder.Configuration.GetSection(SmtpConfigs.SectionName).Get<SmtpConfigs>();
    var appConfigs = builder.Configuration.GetSection(AppConfigs.SectionName).Get<AppConfigs>();

    if (jwtConfigs == null || string.IsNullOrEmpty(jwtConfigs.SecretKey))
        throw new InvalidOperationException("JWT configuration is missing or invalid");
    if (smtpConfigs == null || string.IsNullOrEmpty(smtpConfigs.Host))
        throw new InvalidOperationException("SMTP configuration is missing or invalid");
    if (appConfigs == null || string.IsNullOrEmpty(appConfigs.BaseUrl))
        throw new InvalidOperationException("App configuration is missing or invalid");

    // == Memory Cache ==============================================================
    builder.Services.AddMemoryCache();

    // == Database ==================================================================
    builder.Services.AddDbContext<ApplicationDbContext>(options =>
        options.UseNpgsql(builder.Configuration.GetConnectionString("PostgreSQL")));

    // == Identity ==================================================================
    // Add Identity services
    builder.Services.AddIdentity<UserModel, IdentityRole>(options =>
    {
        // Password settings
        options.Password.RequireDigit = true;
        options.Password.RequiredLength = 8;
        options.Password.RequireNonAlphanumeric = true;
        options.Password.RequireUppercase = true;
        options.Password.RequireLowercase = true;
        options.Password.RequiredUniqueChars = 1;

        // Lockout settings
        options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
        options.Lockout.MaxFailedAccessAttempts = 5;
        options.Lockout.AllowedForNewUsers = true;

        // User settings
        options.User.RequireUniqueEmail = true;
        options.User.AllowedUserNameCharacters = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789-._@+";

        // SignIn settings
        options.SignIn.RequireConfirmedEmail = true;
        options.SignIn.RequireConfirmedPhoneNumber = false;
    })
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddDefaultTokenProviders();

    // Explicitly add RoleManager and UserManager
    builder.Services.AddScoped<RoleManager<IdentityRole>>();
    builder.Services.AddScoped<UserManager<UserModel>>();

    // == JWT Authentication ========================================================
    var key = Encoding.ASCII.GetBytes(jwtConfigs.SecretKey);
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
            IssuerSigningKey = new SymmetricSecurityKey(key),
            ValidateIssuer = true,
            ValidIssuer = jwtConfigs.ValidIssuer,
            ValidateAudience = true,
            ValidAudience = jwtConfigs.ValidAudience,
            ValidateLifetime = true,
            ClockSkew = TimeSpan.Zero,
            RequireExpirationTime = true
        };
    });

    // == Repositories & Services ===================================================
    builder.Services.AddScoped<IAuthRepository, AuthRepository>();
    builder.Services.AddScoped<IAuthService, AuthService>();
    builder.Services.AddScoped<IUserService, UserService>();
    // builder.Services.AddScoped<IAdminService, AdminService>();
    builder.Services.AddScoped<IEmailService, EmailService>();
    builder.Services.AddScoped<EmailService>();
    // builder.Services.AddSingleton<ITokenBlacklistService, TokenBlacklistService>();
    // builder.Services.AddHostedService<TokenBlacklistCleanupService>();

    // == Controllers & Swagger ====================================================
    builder.Services.AddControllers();
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen(options =>
    {
        options.SwaggerDoc("v1", new OpenApiInfo
        {
            Title = "AuthCore API",
            Version = "v1",
            Description = "Authentication REST API built with ASP.NET Core 8 and PostgreSQL"
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

    // == Health Checks =============================================================
    builder.Services.AddHealthChecks()
        .AddDbContextCheck<ApplicationDbContext>("database");

    // == CORS ======================================================================
    builder.Services.AddCors(options =>
    {
        options.AddPolicy("AllowAll", policy =>
        {
            policy.AllowAnyOrigin()
                  .AllowAnyMethod()
                  .AllowAnyHeader();
        });
    });

    // == Build App =================================================================
    var app = builder.Build();

    // == Middleware ================================================================
    app.UseMiddleware<ExceptionHandlingMiddleware>();
    app.UseMiddleware<SecurityHeadersMiddleware>();
    app.UseSerilogRequestLogging();

    if (!app.Environment.IsDevelopment())
        app.UseHttpsRedirection();

    app.UseCors("AllowAll");
    app.UseAuthentication();
    app.UseAuthorization();

    if (app.Environment.IsDevelopment())
    {
        app.UseSwagger();
        app.UseSwaggerUI();
    }

    app.MapControllers();
    app.MapHealthChecks("/health");

    // == Database Migration & Seeding =============================================
    using (var scope = app.Services.CreateScope())
    {
        var services = scope.ServiceProvider;
        try
        {
            var db = services.GetRequiredService<ApplicationDbContext>();
            var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();
            var userManager = services.GetRequiredService<UserManager<UserModel>>();
            var logger = services.GetRequiredService<ILogger<Program>>();
            var seedConfig = services.GetRequiredService<IOptions<SeedConfigs>>().Value;

            // Apply migrations
            logger.LogInformation("Applying database migrations...");
            await db.Database.MigrateAsync();
            logger.LogInformation("Database migrations applied successfully");

            // Seed roles
            logger.LogInformation("Seeding roles...");
            string[] roleNames = { "Admin", "User" };
            foreach (var roleName in roleNames)
            {
                if (!await roleManager.RoleExistsAsync(roleName))
                {
                    var result = await roleManager.CreateAsync(new IdentityRole(roleName));
                    if (result.Succeeded)
                        logger.LogInformation("Role '{Role}' created", roleName);
                    else
                        logger.LogError("Failed to create role '{Role}': {Errors}", roleName,
                            string.Join(", ", result.Errors.Select(e => e.Description)));
                }
            }

            // Seed admin user
            if (seedConfig?.Admin != null && !string.IsNullOrEmpty(seedConfig.Admin.Email))
            {
                logger.LogInformation("Seeding admin user...");
                var adminUser = await userManager.FindByEmailAsync(seedConfig.Admin.Email);

                if (adminUser == null)
                {
                    adminUser = new UserModel
                    {
                        UserName = seedConfig.Admin.Username,
                        Email = seedConfig.Admin.Email,
                        FirstName = seedConfig.Admin.FirstName,
                        LastName = seedConfig.Admin.LastName,
                        EmailConfirmed = true,
                        CreatedAt = DateTime.UtcNow
                    };

                    var result = await userManager.CreateAsync(adminUser, seedConfig.Admin.Password);
                    if (result.Succeeded)
                    {
                        await userManager.AddToRoleAsync(adminUser, "Admin");
                        await userManager.AddToRoleAsync(adminUser, "User");
                        logger.LogInformation("Admin user '{Email}' created", seedConfig.Admin.Email);
                    }
                    else
                    {
                        logger.LogError("Failed to create admin user: {Errors}",
                            string.Join(", ", result.Errors.Select(e => e.Description)));
                    }
                }
                else
                {
                    logger.LogInformation("Admin user already exists");
                }
            }

            logger.LogInformation("Database seeding completed");
        }
        catch (Exception ex)
        {
            var logger = services.GetRequiredService<ILogger<Program>>();
            logger.LogError(ex, "An error occurred while migrating or seeding the database");
            if (app.Environment.IsDevelopment())
                throw;
        }
    }

    app.Run();
}
catch (Exception ex) when (ex is not HostAbortedException)
{
    Log.Fatal(ex, "Application terminated unexpectedly during startup.");
}
finally
{
    Log.CloseAndFlush();
}