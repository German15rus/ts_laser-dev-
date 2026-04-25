using TsLaser.Crm.Api.Domain.Enums;
using TsLaser.Crm.Api.Infrastructure.Repositories;

namespace TsLaser.Crm.Api.Infrastructure.Services;

public sealed class AppointmentStatusUpdaterService(
    AppointmentRepository appointmentRepo,
    ILogger<AppointmentStatusUpdaterService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromMinutes(1));
        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            try
            {
                await UpdateStartedAppointmentsAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Ошибка при автоматическом обновлении статусов записей");
            }
        }
    }

    private async Task UpdateStartedAppointmentsAsync(CancellationToken ct)
    {
        var toUpdate = await appointmentRepo.GetWaitingStartedBeforeAsync(DateTime.UtcNow, ct);
        if (toUpdate.Count == 0) return;

        foreach (var appointment in toUpdate)
        {
            appointment.AppointmentStatus = AppointmentStatus.InProgress;
            await appointmentRepo.UpdateAsync(appointment, ct);
        }

        logger.LogInformation("Автоматически переведено в статус «В работе»: {Count} записей", toUpdate.Count);
    }
}
