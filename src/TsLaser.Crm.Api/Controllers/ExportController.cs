using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TsLaser.Crm.Api.Common;
using TsLaser.Crm.Api.Infrastructure.Persistence;
using TsLaser.Crm.Api.Infrastructure.Services;

namespace TsLaser.Crm.Api.Controllers;

[Authorize]
[ApiController]
[Route("api")]
public sealed class ExportController(AppDbContext dbContext, ExportService exportService) : ControllerBase
{
    [HttpGet("export/clients")]
    public async Task<IActionResult> ExportClients([FromQuery] string format = "csv", CancellationToken cancellationToken = default)
    {
        var clients = await dbContext.Clients.AsNoTracking().OrderBy(x => x.Name).ToListAsync(cancellationToken);
        var partners = await dbContext.Partners.AsNoTracking().ToDictionaryAsync(x => x.Id, x => x.Name, cancellationToken);

        string[] headers =
        [
            "ФИО",
            "Телефон",
            "Дата рождения",
            "Возраст",
            "Пол",
            "Адрес",
            "Как узнали",
            "Статус",
            "Причина ухода",
            "Дата создания",
        ];

        var rows = clients
            .Select(c =>
            {
                var referral = string.Empty;
                if (c.ReferralPartnerId.HasValue && partners.TryGetValue(c.ReferralPartnerId.Value, out var partnerName))
                {
                    referral = partnerName;
                }
                else if (!string.IsNullOrWhiteSpace(c.ReferralCustom))
                {
                    referral = c.ReferralCustom;
                }

                return new[]
                {
                    c.Name ?? string.Empty,
                    c.Phone ?? string.Empty,
                    ExportService.FormatDate(c.BirthDate),
                    c.Age?.ToString() ?? string.Empty,
                    c.Gender ?? string.Empty,
                    c.Address ?? string.Empty,
                    referral,
                    StatusHelper.ToLabel(c.Status),
                    c.StoppedReason ?? string.Empty,
                    ExportService.FormatDateTime(c.CreatedAt),
                };
            })
            .ToList();

        return format.Equals("xlsx", StringComparison.OrdinalIgnoreCase)
            ? exportService.BuildXlsx(headers, rows, "clients")
            : exportService.BuildCsv(headers, rows, "clients");
    }

    [HttpGet("export/partners")]
    public async Task<IActionResult> ExportPartners([FromQuery] string format = "csv", CancellationToken cancellationToken = default)
    {
        var partners = await dbContext.Partners.AsNoTracking().OrderBy(x => x.Name).ToListAsync(cancellationToken);

        string[] headers =
        [
            "Название",
            "Контакты",
            "Тип",
            "Условия",
            "Комментарий",
            "Дата создания",
        ];

        var rows = partners
            .Select(p => new[]
            {
                p.Name ?? string.Empty,
                p.Contacts ?? string.Empty,
                p.Type ?? string.Empty,
                p.Terms ?? string.Empty,
                p.Comment ?? string.Empty,
                ExportService.FormatDateTime(p.CreatedAt),
            })
            .ToList();

        return format.Equals("xlsx", StringComparison.OrdinalIgnoreCase)
            ? exportService.BuildXlsx(headers, rows, "partners")
            : exportService.BuildCsv(headers, rows, "partners");
    }

    [HttpGet("clients/{clientId:int}/export/sessions")]
    public async Task<IActionResult> ExportSessions(int clientId, [FromQuery] string format = "csv", CancellationToken cancellationToken = default)
    {
        var client = await dbContext.Clients.AsNoTracking().FirstOrDefaultAsync(x => x.Id == clientId, cancellationToken);
        if (client is null)
        {
            throw new ApiException(StatusCodes.Status404NotFound, "Клиент не найден");
        }

        var sessions = await dbContext.LaserSessions
            .AsNoTracking()
            .Where(x => x.ClientId == clientId)
            .OrderBy(x => x.SessionDate)
            .ToListAsync(cancellationToken);

        var tattoos = await dbContext.Tattoos
            .AsNoTracking()
            .Where(x => x.ClientId == clientId)
            .ToDictionaryAsync(x => x.Id, x => x.Name, cancellationToken);

        string[] headers =
        [
            "Татуировка/Татуаж",
            "№ сеанса",
            "Участок",
            "Длина волны",
            "Диаметр",
            "Плотность",
            "Герц",
            "Вспышки",
            "Дата сеанса",
            "Перерыв",
            "Комментарий",
        ];

        var rows = sessions
            .Select(s =>
            {
                var tattooName = string.Empty;
                if (s.TattooId.HasValue && tattoos.TryGetValue(s.TattooId.Value, out var name))
                {
                    tattooName = name;
                }
                else if (!string.IsNullOrWhiteSpace(s.TattooName))
                {
                    tattooName = s.TattooName;
                }

                return new[]
                {
                    tattooName,
                    s.SessionNumber?.ToString() ?? string.Empty,
                    s.SubSession ?? string.Empty,
                    s.Wavelength ?? string.Empty,
                    s.Diameter ?? string.Empty,
                    s.Density ?? string.Empty,
                    s.Hertz ?? string.Empty,
                    s.FlashesCount?.ToString() ?? string.Empty,
                    ExportService.FormatDate(s.SessionDate),
                    s.BreakPeriod ?? string.Empty,
                    s.Comment ?? string.Empty,
                };
            })
            .ToList();

        var filename = $"sessions_{client.Name.Replace(' ', '_')}";

        return format.Equals("xlsx", StringComparison.OrdinalIgnoreCase)
            ? exportService.BuildXlsx(headers, rows, filename)
            : exportService.BuildCsv(headers, rows, filename);
    }

    [HttpGet("clients/{clientId:int}/export/tattoos")]
    public async Task<IActionResult> ExportTattoos(int clientId, [FromQuery] string format = "csv", CancellationToken cancellationToken = default)
    {
        var client = await dbContext.Clients.AsNoTracking().FirstOrDefaultAsync(x => x.Id == clientId, cancellationToken);
        if (client is null)
        {
            throw new ApiException(StatusCodes.Status404NotFound, "Клиент не найден");
        }

        var tattoos = await dbContext.Tattoos
            .AsNoTracking()
            .Where(x => x.ClientId == clientId)
            .OrderBy(x => x.Name)
            .ToListAsync(cancellationToken);

        string[] headers =
        [
            "Название",
            "Зона удаления",
            "Коррекций",
            "Последний пигмент",
            "Последний лазер",
            "Не удалял ранее",
            "Где удаляли",
            "Желаемый результат",
            "Дата создания",
        ];

        var rows = tattoos
            .Select(t => new[]
            {
                t.Name ?? string.Empty,
                t.RemovalZone ?? string.Empty,
                t.CorrectionsCount ?? string.Empty,
                ExportService.FormatDate(t.LastPigmentDate),
                ExportService.FormatDate(t.LastLaserDate),
                t.NoLaserBefore ? "Да" : "Нет",
                t.PreviousRemovalPlace ?? string.Empty,
                t.DesiredResult ?? string.Empty,
                ExportService.FormatDateTime(t.CreatedAt),
            })
            .ToList();

        var filename = $"tattoos_{client.Name.Replace(' ', '_')}";

        return format.Equals("xlsx", StringComparison.OrdinalIgnoreCase)
            ? exportService.BuildXlsx(headers, rows, filename)
            : exportService.BuildCsv(headers, rows, filename);
    }
}
