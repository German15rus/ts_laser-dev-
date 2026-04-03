using Microsoft.EntityFrameworkCore;

namespace TsLaser.Crm.Api.Infrastructure.Persistence;

public sealed class DbInitializer(AppDbContext dbContext, ILogger<DbInitializer> logger)
{
    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        var dataDir = Path.Combine(Directory.GetCurrentDirectory(), "Data");
        Directory.CreateDirectory(dataDir);

        logger.LogInformation("Applying database migrations");
        await dbContext.Database.MigrateAsync(cancellationToken);
    }
}
