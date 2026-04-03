using System.Text.Json;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Serilog;
using TsLaser.Crm.Api.Infrastructure.Persistence;
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

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddScoped<DbInitializer>();
builder.Services.AddScoped<LegacyImportService>();
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

string? importPath = null;
for (var i = 0; i < args.Length; i++)
{
    if (args[i].Equals("--import-legacy", StringComparison.OrdinalIgnoreCase))
    {
        if (i + 1 < args.Length && !args[i + 1].StartsWith("--", StringComparison.Ordinal))
        {
            importPath = ResolveLegacyPath(args[i + 1]);
        }
        else
        {
            importPath = ResolveLegacyPath("legacy_python_backend/tslaser.db");
        }

        break;
    }
}

using (var scope = app.Services.CreateScope())
{
    var initializer = scope.ServiceProvider.GetRequiredService<DbInitializer>();
    await initializer.InitializeAsync();
}

if (!string.IsNullOrWhiteSpace(importPath))
{
    if (!File.Exists(importPath))
    {
        throw new FileNotFoundException(
            "Legacy database file was not found. Expected tslaser.db. Pass explicit path: --import-legacy <path-to-tslaser.db>",
            importPath);
    }

    Log.Information("Resolved legacy import path: {ImportPath}", importPath);

    using var importScope = app.Services.CreateScope();
    var importer = importScope.ServiceProvider.GetRequiredService<LegacyImportService>();
    await importer.ImportAsync(importPath);
    return;
}

await app.RunAsync();

static string ResolveLegacyPath(string rawPath)
{
    if (string.IsNullOrWhiteSpace(rawPath))
    {
        return rawPath;
    }

    if (Path.IsPathRooted(rawPath))
    {
        return Path.GetFullPath(rawPath);
    }

    var cwd = Directory.GetCurrentDirectory();

    var candidates = new[]
    {
        Path.GetFullPath(Path.Combine(cwd, rawPath)),
        Path.GetFullPath(Path.Combine(cwd, "..", rawPath)),
        Path.GetFullPath(Path.Combine(cwd, "..", "..", rawPath)),
        Path.GetFullPath(Path.Combine(cwd, "..", "..", "..", rawPath)),
        Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, rawPath)),
    };

    return candidates.FirstOrDefault(File.Exists) ?? candidates[0];
}
