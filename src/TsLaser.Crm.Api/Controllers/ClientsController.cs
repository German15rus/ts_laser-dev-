using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TsLaser.Crm.Api.Common;
using TsLaser.Crm.Api.Contracts;
using TsLaser.Crm.Api.Domain.Entities;
using TsLaser.Crm.Api.Domain.Enums;
using TsLaser.Crm.Api.Infrastructure.Repositories;

namespace TsLaser.Crm.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/clients")]
public sealed class ClientsController(
    ClientRepository clientRepo,
    PartnerRepository partnerRepo,
    IntakeSubmissionRepository submissionRepo) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<List<ClientListResponse>>> GetClients(
        [FromQuery] string? search,
        [FromQuery(Name = "status_filter")] string? statusFilter,
        [FromQuery(Name = "partner_filter")] int? partnerFilter,
        [FromQuery(Name = "sort_by")] string sortBy = "name",
        [FromQuery(Name = "sort_order")] string sortOrder = "asc",
        CancellationToken cancellationToken = default)
    {
        var clients = await clientRepo.GetAllAsync(statusFilter, partnerFilter, sortBy, sortOrder, cancellationToken);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var searchLower = search.ToLowerInvariant();
            var searchDigits = new string(search.Where(char.IsDigit).ToArray());

            clients = clients
                .Where(client =>
                    (!string.IsNullOrWhiteSpace(client.Name) && client.Name.ToLowerInvariant().Contains(searchLower)) ||
                    (!string.IsNullOrWhiteSpace(searchDigits) && !string.IsNullOrWhiteSpace(client.Phone) && client.Phone.Contains(searchDigits)))
                .ToList();
        }

        return Ok(clients.Select(x => x.ToListResponse()).ToList());
    }

    [HttpPost]
    public async Task<ActionResult<ClientResponse>> CreateClient([FromBody] ClientCreateRequest request, CancellationToken cancellationToken)
    {
        var client = new Client
        {
            Name = request.Name.Trim(),
            Phone = request.Phone,
            BirthDate = request.BirthDate,
            Age = request.Age,
            Gender = request.Gender,
            Address = request.Address,
            ReferralPartnerId = request.ReferralPartnerId,
            ReferralCustom = request.ReferralCustom,
            Status = StatusHelper.Normalize(request.Status),
            StoppedReason = request.StoppedReason,
        };

        await clientRepo.CreateAsync(client, cancellationToken);

        string? partnerName = null;
        if (client.ReferralPartnerId.HasValue)
        {
            partnerName = (await partnerRepo.GetByIdAsync(client.ReferralPartnerId.Value, cancellationToken))?.Name;
        }

        return CreatedAtAction(nameof(GetClient), new { clientId = client.Id }, client.ToResponse(partnerName));
    }

    [HttpGet("{clientId:int}")]
    public async Task<ActionResult<ClientResponse>> GetClient(int clientId, CancellationToken cancellationToken)
    {
        var client = await clientRepo.GetByIdAsync(clientId, cancellationToken);
        if (client is null)
        {
            throw new ApiException(StatusCodes.Status404NotFound, "Клиент не найден");
        }

        string? partnerName = null;
        if (client.ReferralPartnerId.HasValue)
        {
            partnerName = (await partnerRepo.GetByIdAsync(client.ReferralPartnerId.Value, cancellationToken))?.Name;
        }

        return Ok(client.ToResponse(partnerName));
    }

    [HttpPut("{clientId:int}")]
    public async Task<ActionResult<ClientResponse>> UpdateClient(int clientId, [FromBody] ClientUpdateRequest request, CancellationToken cancellationToken)
    {
        var client = await clientRepo.GetByIdAsync(clientId, cancellationToken);
        if (client is null)
        {
            throw new ApiException(StatusCodes.Status404NotFound, "Клиент не найден");
        }

        if (!string.IsNullOrWhiteSpace(request.Name))
        {
            client.Name = request.Name.Trim();
        }

        client.Phone = request.Phone;
        client.BirthDate = request.BirthDate;
        client.Age = request.Age;
        client.Gender = request.Gender;
        client.Address = request.Address;
        client.ReferralPartnerId = request.ReferralPartnerId;
        client.ReferralCustom = request.ReferralCustom;
        client.Status = StatusHelper.Normalize(request.Status ?? client.Status);
        client.StoppedReason = request.StoppedReason;

        await clientRepo.UpdateAsync(client, cancellationToken);

        if (client.Status == "completed")
        {
            var submissions = await submissionRepo.GetApprovedByClientIdAsync(clientId, cancellationToken);
            foreach (var sub in submissions)
            {
                sub.Status = IntakeSubmissionStatus.Completed;
                await submissionRepo.UpdateAsync(sub, cancellationToken);
            }
        }

        string? partnerName = null;
        if (client.ReferralPartnerId.HasValue)
        {
            partnerName = (await partnerRepo.GetByIdAsync(client.ReferralPartnerId.Value, cancellationToken))?.Name;
        }

        return Ok(client.ToResponse(partnerName));
    }

    [HttpDelete("{clientId:int}")]
    public async Task<IActionResult> DeleteClient(int clientId, CancellationToken cancellationToken)
    {
        var client = await clientRepo.GetByIdAsync(clientId, cancellationToken);
        if (client is null)
        {
            throw new ApiException(StatusCodes.Status404NotFound, "Клиент не найден");
        }

        await clientRepo.DeleteAsync(clientId, cancellationToken);
        return Ok(new { success = true, message = "Клиент удален" });
    }
}
