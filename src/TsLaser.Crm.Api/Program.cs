using System.Text.Json;
using Google.Apis.Auth.OAuth2;
using Google.Cloud.Firestore;
using Google.Cloud.Firestore.V1;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Serilog;
using TsLaser.Crm.Api.Infrastructure.Repositories;
using TsLaser.Crm.Api.Infrastructure.Security;
using TsLaser.Crm.Api.Infrastructure.Services;
using TsLaser.Crm.Api.Middleware;
using TsLaser.Crm.Api.Options;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((context, configuration) =>
{
    configuration
        .ReadFrom.Configuration(context.Configuration)
        .WriteTo.Console();
});

builder.Services.Configure<AuthOptions>(builder.Configuration.GetSection(AuthOptions.SectionName));

builder.Services
    .AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower;
        options.JsonSerializerOptions.DictionaryKeyPolicy = JsonNamingPolicy.SnakeCaseLower;
    });

builder.Services.Configure<ApiBehaviorOptions>(options =>
{
    options.InvalidModelStateResponseFactory = _ =>
        new BadRequestObjectResult(new { detail = "Validation failed" });
});

builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.Cookie.Name = "tslaser_session";
        options.Cookie.HttpOnly = true;
        options.Cookie.SameSite = SameSiteMode.Lax;
        options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
        options.ExpireTimeSpan = TimeSpan.FromDays(7);
        options.SlidingExpiration = true;

        options.Events = new CookieAuthenticationEvents
        {
            OnRedirectToLogin = context =>
            {
                if (context.Request.Path.StartsWithSegments("/api"))
                {
                    context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                    return context.Response.WriteAsJsonAsync(new { detail = "Not authenticated" });
                }

                context.Response.Redirect("/login");
                return Task.CompletedTask;
            },
            OnRedirectToAccessDenied = context =>
            {
                if (context.Request.Path.StartsWithSegments("/api"))
                {
                    context.Response.StatusCode = StatusCodes.Status403Forbidden;
                    return context.Response.WriteAsJsonAsync(new { detail = "Forbidden" });
                }

                context.Response.Redirect("/login");
                return Task.CompletedTask;
            }
        };
    });

builder.Services.AddAuthorization();

builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.OnRejected = async (context, token) =>
    {
        context.HttpContext.Response.ContentType = "application/json";
        await context.HttpContext.Response.WriteAsJsonAsync(new { detail = "Too many requests" }, cancellationToken: token);
    };

    options.AddFixedWindowLimiter("login", o =>
    {
        o.PermitLimit = 20;
        o.Window = TimeSpan.FromMinutes(1);
        o.QueueLimit = 0;
        o.AutoReplenishment = true;
    });

    options.AddFixedWindowLimiter("booking", o =>
    {
        o.PermitLimit = 10;
        o.Window = TimeSpan.FromMinutes(1);
        o.QueueLimit = 0;
        o.AutoReplenishment = true;
    });
});

// Firestore
builder.Services.AddSingleton<FirestoreDb>(sp =>
{
    var config = sp.GetRequiredService<IConfiguration>();
    var logger = sp.GetRequiredService<ILogger<FirestoreDb>>();

    var keyJson = Environment.GetEnvironmentVariable("FIREBASE_CREDENTIALS_JSON");

    if (string.IsNullOrWhiteSpace(keyJson))
    {
        var credentialsPath = config["Firebase:CredentialsPath"] ?? "firebase-key.json";
        if (!File.Exists(credentialsPath))
        {
            throw new InvalidOperationException(
                $"Firebase credentials not found. Set FIREBASE_CREDENTIALS_JSON env var or provide file at '{credentialsPath}'.");
        }
        keyJson = File.ReadAllText(credentialsPath);
    }

    var keyDoc = JsonDocument.Parse(keyJson);
    var projectId = keyDoc.RootElement.GetProperty("project_id").GetString()
        ?? throw new InvalidOperationException("project_id not found in Firebase credentials.");

    using var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(keyJson));
    var credential = GoogleCredential.FromStream(stream);
    var client = new FirestoreClientBuilder { Credential = credential }.Build();
    var db = FirestoreDb.Create(projectId, client);

    logger.LogInformation("Firestore initialized for project {ProjectId}.", projectId);
    return db;
});

// Repositories
builder.Services.AddSingleton<FirestoreCounterRepository>();
builder.Services.AddSingleton<PartnerRepository>();
builder.Services.AddSingleton<ClientRepository>();
builder.Services.AddSingleton<TattooRepository>();
builder.Services.AddSingleton<LaserSessionRepository>();
builder.Services.AddSingleton<IntakeSubmissionRepository>();
builder.Services.AddSingleton<AppointmentRepository>();

// Services
builder.Services.AddScoped<BookingModerationService>();
builder.Services.AddScoped<FirebaseMigrationService>();
builder.Services.AddSingleton<IPasswordService, PasswordService>();
builder.Services.AddSingleton<TemplateService>();
builder.Services.AddSingleton<ExportService>();

builder.Services.AddOpenApi();

var app = builder.Build();

app.UseSerilogRequestLogging();
app.UseMiddleware<ApiExceptionMiddleware>();

app.UseStaticFiles();
app.UseRouting();
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

// CLI: --migrate-to-firebase [path-to-tslaser.db]
string? migratePath = null;
for (var i = 0; i < args.Length; i++)
{
    if (args[i].Equals("--migrate-to-firebase", StringComparison.OrdinalIgnoreCase))
    {
        migratePath = i + 1 < args.Length && !args[i + 1].StartsWith("--", StringComparison.Ordinal)
            ? ResolvePath(args[i + 1])
            : ResolvePath("Data/tslaser.db");
        break;
    }
}

if (!string.IsNullOrWhiteSpace(migratePath))
{
    if (!File.Exists(migratePath))
    {
        throw new FileNotFoundException(
            "SQLite database file not found. Pass explicit path: --migrate-to-firebase <path-to-tslaser.db>",
            migratePath);
    }

    Log.Information("Migrating from SQLite: {Path}", migratePath);

    using var migrationScope = app.Services.CreateScope();
    var migrationService = migrationScope.ServiceProvider.GetRequiredService<FirebaseMigrationService>();
    await migrationService.MigrateAsync(migratePath);
    Log.Information("Migration complete. You can now start the app without --migrate-to-firebase.");
    return;
}

await app.RunAsync();

static string ResolvePath(string rawPath)
{
    if (string.IsNullOrWhiteSpace(rawPath)) return rawPath;
    if (Path.IsPathRooted(rawPath)) return Path.GetFullPath(rawPath);

    var cwd = Directory.GetCurrentDirectory();
    var candidates = new[]
    {
        Path.GetFullPath(Path.Combine(cwd, rawPath)),
        Path.GetFullPath(Path.Combine(cwd, "..", rawPath)),
        Path.GetFullPath(Path.Combine(cwd, "..", "..", rawPath)),
        Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, rawPath)),
    };
    return candidates.FirstOrDefault(File.Exists) ?? candidates[0];
}
