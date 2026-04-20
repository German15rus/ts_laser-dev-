using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TsLaser.Crm.Api.Common;
using TsLaser.Crm.Api.Contracts;
using TsLaser.Crm.Api.Domain.Entities;
using TsLaser.Crm.Api.Infrastructure.Repositories;

namespace TsLaser.Crm.Api.Controllers;

[Authorize]
[ApiController]
public sealed class TattoosController(ClientRepository clientRepo, TattooRepository tattooRepo) : ControllerBase
{
    [HttpGet("api/clients/{clientId:int}/tattoos")]
    public async Task<ActionResult<TattoosListResponse>> GetClientTattoos(int clientId, CancellationToken cancellationToken)
    {
        var client = await clientRepo.GetByIdAsync(clientId, cancellationToken);
        if (client is null)
        {
            throw new ApiException(StatusCodes.Status404NotFound, "Клиент не найден");
        }

        var tattoos = await tattooRepo.GetByClientIdAsync(clientId, cancellationToken);

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
        var clientExists = await clientRepo.ExistsAsync(clientId, cancellationToken);
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

        await tattooRepo.CreateAsync(tattoo, cancellationToken);
        return CreatedAtAction(nameof(GetTattoo), new { tattooId = tattoo.Id }, tattoo.ToResponse());
    }

    [HttpGet("api/tattoos/{tattooId:int}")]
    public async Task<ActionResult<TattooResponse>> GetTattoo(int tattooId, CancellationToken cancellationToken)
    {
        var tattoo = await tattooRepo.GetByIdAsync(tattooId, cancellationToken);
        if (tattoo is null)
        {
            throw new ApiException(StatusCodes.Status404NotFound, "Татуировка не найдена");
        }

        return Ok(tattoo.ToResponse());
    }

    [HttpPut("api/tattoos/{tattooId:int}")]
    public async Task<ActionResult<TattooResponse>> UpdateTattoo(int tattooId, [FromBody] TattooUpdateRequest request, CancellationToken cancellationToken)
    {
        var tattoo = await tattooRepo.GetByIdAsync(tattooId, cancellationToken);
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

        await tattooRepo.UpdateAsync(tattoo, cancellationToken);
        return Ok(tattoo.ToResponse());
    }

    [HttpDelete("api/tattoos/{tattooId:int}")]
    public async Task<IActionResult> DeleteTattoo(int tattooId, CancellationToken cancellationToken)
    {
        var tattoo = await tattooRepo.GetByIdAsync(tattooId, cancellationToken);
        if (tattoo is null)
        {
            throw new ApiException(StatusCodes.Status404NotFound, "Татуировка не найдена");
        }

        await tattooRepo.DeleteAsync(tattooId, cancellationToken);
        return Ok(new { success = true, message = "Татуировка удалена" });
    }
}
