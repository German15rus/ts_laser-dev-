using Microsoft.EntityFrameworkCore;
using TsLaser.Crm.Api.Infrastructure.Services;

namespace TsLaser.Crm.Api.Infrastructure.Persistence;

public sealed class DbInitializer(AppDbContext dbContext, FirestoreService firestoreService, ILogger<DbInitializer> logger)
{
    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        var dataDir = Path.Combine(Directory.GetCurrentDirectory(), "Data");
        Directory.CreateDirectory(dataDir);

        logger.LogInformation("Applying database migrations");
        await dbContext.Database.MigrateAsync(cancellationToken);

        logger.LogInformation("Restoring missing submissions from Firestore");
        await firestoreService.RestoreMissingSubmissionsAsync(dbContext, cancellationToken);
    }
}
