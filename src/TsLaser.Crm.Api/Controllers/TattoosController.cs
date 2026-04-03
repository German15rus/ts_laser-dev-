using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TsLaser.Crm.Api.Common;
using TsLaser.Crm.Api.Contracts;
using TsLaser.Crm.Api.Domain.Entities;
using TsLaser.Crm.Api.Infrastructure.Persistence;

namespace TsLaser.Crm.Api.Controllers;

[Authorize]
[ApiController]
public sealed class TattoosController(AppDbContext dbContext) : ControllerBase
{
    [HttpGet("api/clients/{clientId:int}/tattoos")]
    public async Task<ActionResult<TattoosListResponse>> GetClientTattoos(int clientId, CancellationToken cancellationToken)
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

        return Ok(new TattoosListResponse
        {
            Tattoos = tattoos.Select(x => x.ToResponse()).ToList(),
            ClientName = client.Name,
            ClientId = client.Id,
        });
    }

    [HttpPost("api/clients/{clientId:int}/tattoos")]
    public async Task<ActionResult<TattooResponse>> CreateTattoo(int clientId, [FromBody] TattooCreateRequest request, CancellationToken cancellationToken)
    {
        var clientExists = await dbContext.Clients.AnyAsync(x => x.Id == clientId, cancellationToken);
        if (!clientExists)
        {
            throw new ApiException(StatusCodes.Status404NotFound, "Клиент не найден");
        }

        var tattoo = new Tattoo
        {
            ClientId = clientId,
            Name = request.Name.Trim(),
            RemovalZone = request.RemovalZone,
            CorrectionsCount = request.CorrectionsCount,
            LastPigmentDate = request.LastPigmentDate,
            LastLaserDate = request.LastLaserDate,
            NoLaserBefore = request.NoLaserBefore,
            PreviousRemovalPlace = request.PreviousRemovalPlace,
            DesiredResult = request.DesiredResult,
        };

        dbContext.Tattoos.Add(tattoo);
        await dbContext.SaveChangesAsync(cancellationToken);

        return CreatedAtAction(nameof(GetTattoo), new { tattooId = tattoo.Id }, tattoo.ToResponse());
    }

    [HttpGet("api/tattoos/{tattooId:int}")]
    public async Task<ActionResult<TattooResponse>> GetTattoo(int tattooId, CancellationToken cancellationToken)
    {
        var tattoo = await dbContext.Tattoos.AsNoTracking().FirstOrDefaultAsync(x => x.Id == tattooId, cancellationToken);
        if (tattoo is null)
        {
            throw new ApiException(StatusCodes.Status404NotFound, "Татуировка не найдена");
        }

        return Ok(tattoo.ToResponse());
    }

    [HttpPut("api/tattoos/{tattooId:int}")]
    public async Task<ActionResult<TattooResponse>> UpdateTattoo(int tattooId, [FromBody] TattooUpdateRequest request, CancellationToken cancellationToken)
    {
        var tattoo = await dbContext.Tattoos.FirstOrDefaultAsync(x => x.Id == tattooId, cancellationToken);
        if (tattoo is null)
        {
            throw new ApiException(StatusCodes.Status404NotFound, "Татуировка не найдена");
        }

        if (!string.IsNullOrWhiteSpace(request.Name))
        {
            tattoo.Name = request.Name.Trim();
        }

        tattoo.RemovalZone = request.RemovalZone;
        tattoo.CorrectionsCount = request.CorrectionsCount;
        tattoo.LastPigmentDate = request.LastPigmentDate;
        tattoo.LastLaserDate = request.LastLaserDate;
        tattoo.NoLaserBefore = request.NoLaserBefore ?? tattoo.NoLaserBefore;
        tattoo.PreviousRemovalPlace = request.PreviousRemovalPlace;
        tattoo.DesiredResult = request.DesiredResult;

        await dbContext.SaveChangesAsync(cancellationToken);

        return Ok(tattoo.ToResponse());
    }

    [HttpDelete("api/tattoos/{tattooId:int}")]
    public async Task<IActionResult> DeleteTattoo(int tattooId, CancellationToken cancellationToken)
    {
        var tattoo = await dbContext.Tattoos.FirstOrDefaultAsync(x => x.Id == tattooId, cancellationToken);
        if (tattoo is null)
        {
            throw new ApiException(StatusCodes.Status404NotFound, "Татуировка не найдена");
        }

        dbContext.Tattoos.Remove(tattoo);
        await dbContext.SaveChangesAsync(cancellationToken);

        return Ok(new { success = true, message = "Татуировка удалена" });
    }
}
