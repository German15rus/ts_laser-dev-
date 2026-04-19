using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TsLaser.Crm.Api.Common;
using TsLaser.Crm.Api.Contracts;
using TsLaser.Crm.Api.Domain.Entities;
using TsLaser.Crm.Api.Domain.Enums;
using TsLaser.Crm.Api.Infrastructure.Persistence;

namespace TsLaser.Crm.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/calendar")]
public sealed class CalendarController(AppDbContext dbContext) : ControllerBase
{
    private static readonly int[] WorkingDays = [2, 4, 6]; // Tue, Thu, Sat
    private const int WorkDayStartHour = 10;
    private const int WorkDayEndHour = 20;

    [HttpGet("appointments")]
    public async Task<ActionResult<List<AppointmentResponse>>> GetAppointments(
        [FromQuery] DateOnly from,
        [FromQuery] DateOnly to,
        CancellationToken cancellationToken = default)
    {
        var fromUtc = from.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
        var toUtc = to.ToDateTime(new TimeOnly(23, 59, 59), DateTimeKind.Utc);

        var appointments = await dbContext.Appointments
            .AsNoTracking()
            .Include(x => x.IntakeSubmission)
            .Where(x => x.StartTime >= fromUtc && x.StartTime <= toUtc)
            .OrderBy(x => x.StartTime)
            .ToListAsync(cancellationToken);

        return Ok(appointments.Select(ToResponse).ToList());
    }

    [HttpGet("available-clients")]
    public async Task<ActionResult<List<AvailableClientResponse>>> GetAvailableClients(
        CancellationToken cancellationToken = default)
    {
        var scheduledIds = await dbContext.Appointments
            .AsNoTracking()
            .Select(x => x.IntakeSubmissionId)
            .ToListAsync(cancellationToken);

        var clients = await dbContext.IntakeSubmissions
            .AsNoTracking()
            .Where(x => x.Status == IntakeSubmissionStatus.Approved && !scheduledIds.Contains(x.Id))
            .OrderByDescending(x => x.CreatedAt)
            .Select(x => new AvailableClientResponse
            {
                SubmissionId = x.Id,
                FullName = x.FullName,
                TattooType = x.TattooType,
                Phone = x.Phone,
            })
            .ToListAsync(cancellationToken);

        return Ok(clients);
    }

    [HttpPost("appointments")]
    public async Task<ActionResult<AppointmentResponse>> CreateAppointment(
        [FromBody] CreateAppointmentRequest request,
        CancellationToken cancellationToken = default)
    {
        ValidateWorkSchedule(request.StartTime, request.DurationMinutes);

        var submission = await dbContext.IntakeSubmissions
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == request.IntakeSubmissionId, cancellationToken);

        if (submission is null)
        {
            throw new ApiException(StatusCodes.Status404NotFound, "Анкета не найдена");
        }

        if (submission.Status != IntakeSubmissionStatus.Approved)
        {
            throw new ApiException(StatusCodes.Status400BadRequest, "Только подтверждённые анкеты можно записывать");
        }

        var alreadyScheduled = await dbContext.Appointments
            .AnyAsync(x => x.IntakeSubmissionId == request.IntakeSubmissionId, cancellationToken);

        if (alreadyScheduled)
        {
            throw new ApiException(StatusCodes.Status409Conflict, "Этот клиент уже записан");
        }

        if (string.IsNullOrWhiteSpace(request.MasterName))
        {
            throw new ApiException(StatusCodes.Status400BadRequest, "Укажите имя мастера");
        }

        var appointment = new Appointment
        {
            IntakeSubmissionId = request.IntakeSubmissionId,
            MasterName = request.MasterName.Trim(),
            StartTime = request.StartTime.ToUniversalTime(),
            DurationMinutes = request.DurationMinutes,
            AppointmentStatus = AppointmentStatus.Waiting,
        };

        dbContext.Appointments.Add(appointment);
        await dbContext.SaveChangesAsync(cancellationToken);

        await dbContext.Entry(appointment)
            .Reference(x => x.IntakeSubmission)
            .LoadAsync(cancellationToken);

        return CreatedAtAction(nameof(GetAppointments), ToResponse(appointment));
    }

    [HttpPut("appointments/{id:int}")]
    public async Task<ActionResult<AppointmentResponse>> UpdateAppointment(
        int id,
        [FromBody] UpdateAppointmentRequest request,
        CancellationToken cancellationToken = default)
    {
        var appointment = await dbContext.Appointments
            .Include(x => x.IntakeSubmission)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

        if (appointment is null)
        {
            throw new ApiException(StatusCodes.Status404NotFound, "Запись не найдена");
        }

        var newStartTime = request.StartTime.HasValue
            ? request.StartTime.Value
            : appointment.StartTime;

        var newDuration = request.DurationMinutes.HasValue
            ? request.DurationMinutes.Value
            : appointment.DurationMinutes;

        if (request.StartTime.HasValue || request.DurationMinutes.HasValue)
        {
            ValidateWorkSchedule(newStartTime, newDuration);
        }

        if (request.StartTime.HasValue)
        {
            appointment.StartTime = request.StartTime.Value.ToUniversalTime();
        }

        if (request.DurationMinutes.HasValue)
        {
            appointment.DurationMinutes = request.DurationMinutes.Value;
        }

        if (!string.IsNullOrWhiteSpace(request.MasterName))
        {
            appointment.MasterName = request.MasterName.Trim();
        }

        if (!string.IsNullOrWhiteSpace(request.AppointmentStatus))
        {
            if (!AppointmentStatus.IsAllowed(request.AppointmentStatus))
            {
                throw new ApiException(StatusCodes.Status400BadRequest, "Некорректный статус записи");
            }

            appointment.AppointmentStatus = AppointmentStatus.Normalize(request.AppointmentStatus);
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        return Ok(ToResponse(appointment));
    }

    [HttpDelete("appointments/{id:int}")]
    public async Task<IActionResult> DeleteAppointment(int id, CancellationToken cancellationToken = default)
    {
        var appointment = await dbContext.Appointments
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

        if (appointment is null)
        {
            throw new ApiException(StatusCodes.Status404NotFound, "Запись не найдена");
        }

        dbContext.Appointments.Remove(appointment);
        await dbContext.SaveChangesAsync(cancellationToken);

        return NoContent();
    }

    private static void ValidateWorkSchedule(DateTime startTime, int durationMinutes)
    {
        var localTime = startTime.Kind == DateTimeKind.Utc
            ? startTime.ToLocalTime()
            : startTime;

        if (!WorkingDays.Contains((int)localTime.DayOfWeek))
        {
            throw new ApiException(StatusCodes.Status400BadRequest,
                "Рабочие дни: Вторник, Четверг, Суббота");
        }

        if (localTime.Hour < WorkDayStartHour)
        {
            throw new ApiException(StatusCodes.Status400BadRequest,
                $"Рабочее время с {WorkDayStartHour}:00 до {WorkDayEndHour}:00");
        }

        var endTime = localTime.AddMinutes(durationMinutes);
        var workDayEnd = localTime.Date.AddHours(WorkDayEndHour);

        if (endTime > workDayEnd)
        {
            throw new ApiException(StatusCodes.Status400BadRequest,
                $"Сеанс заканчивается в {endTime:HH:mm}, рабочий день до {WorkDayEndHour}:00");
        }

        if (durationMinutes < 1 || durationMinutes > 659)
        {
            throw new ApiException(StatusCodes.Status400BadRequest,
                "Длительность: от 1 до 659 минут (до 10 ч 59 мин)");
        }
    }

    private static AppointmentResponse ToResponse(Appointment a) => new()
    {
        Id = a.Id,
        IntakeSubmissionId = a.IntakeSubmissionId,
        ClientName = a.IntakeSubmission.FullName,
        Service = a.IntakeSubmission.TattooType,
        MasterName = a.MasterName,
        StartTime = a.StartTime,
        DurationMinutes = a.DurationMinutes,
        EndTime = a.StartTime.AddMinutes(a.DurationMinutes),
        AppointmentStatus = a.AppointmentStatus,
        CreatedAt = a.CreatedAt,
        UpdatedAt = a.UpdatedAt,
    };
}
