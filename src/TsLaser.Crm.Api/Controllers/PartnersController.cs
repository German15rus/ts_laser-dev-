using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TsLaser.Crm.Api.Common;
using TsLaser.Crm.Api.Contracts;
using TsLaser.Crm.Api.Domain.Entities;
using TsLaser.Crm.Api.Infrastructure.Repositories;

namespace TsLaser.Crm.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/partners")]
public sealed class PartnersController(PartnerRepository partnerRepo) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<List<PartnerResponse>>> GetPartners(
        [FromQuery] string? search,
        [FromQuery(Name = "type_filter")] string? typeFilter,
        [FromQuery(Name = "sort_by")] string sortBy = "name",
        [FromQuery(Name = "sort_order")] string sortOrder = "asc",
        CancellationToken cancellationToken = default)
    {
        var items = await partnerRepo.GetAllAsync(typeFilter, sortBy, sortOrder, cancellationToken);

        if (!string.IsNullOrWhiteSpace(search))
        {
            items = items
                .Where(x => (x.Name ?? string.Empty).Contains(search, StringComparison.CurrentCultureIgnoreCase))
                .ToList();
        }

        return Ok(items.Select(x => x.ToResponse()).ToList());
    }

    [HttpPost]
    public async Task<ActionResult<PartnerResponse>> CreatePartner([FromBody] PartnerCreateRequest request, CancellationToken cancellationToken)
    {
        var partner = new Partner
        {
            Name = request.Name.Trim(),
            Contacts = request.Contacts?.Trim(),
            Type = request.Type?.Trim(),
            Terms = request.Terms?.Trim(),
            Comment = request.Comment?.Trim(),
        };

        await partnerRepo.CreateAsync(partner, cancellationToken);
        return CreatedAtAction(nameof(GetPartner), new { partnerId = partner.Id }, partner.ToResponse());
    }

    [HttpGet("{partnerId:int}")]
    public async Task<ActionResult<PartnerResponse>> GetPartner(int partnerId, CancellationToken cancellationToken)
    {
        var partner = await partnerRepo.GetByIdAsync(partnerId, cancellationToken);
        if (partner is null)
        {
            throw new ApiException(StatusCodes.Status404NotFound, "Партнер не найден");
        }

        return Ok(partner.ToResponse());
    }

    [HttpPut("{partnerId:int}")]
    public async Task<ActionResult<PartnerResponse>> UpdatePartner(int partnerId, [FromBody] PartnerUpdateRequest request, CancellationToken cancellationToken)
    {
        var partner = await partnerRepo.GetByIdAsync(partnerId, cancellationToken);
        if (partner is null)
        {
            throw new ApiException(StatusCodes.Status404NotFound, "Партнер не найден");
        }

        if (!string.IsNullOrWhiteSpace(request.Name))
        {
            partner.Name = request.Name.Trim();
        }

        partner.Contacts = request.Contacts?.Trim();
        partner.Type = request.Type?.Trim();
        partner.Terms = request.Terms?.Trim();
        partner.Comment = request.Comment?.Trim();

        await partnerRepo.UpdateAsync(partner, cancellationToken);
        return Ok(partner.ToResponse());
    }

    [HttpDelete("{partnerId:int}")]
    public async Task<IActionResult> DeletePartner(int partnerId, CancellationToken cancellationToken)
    {
        var partner = await partnerRepo.GetByIdAsync(partnerId, cancellationToken);
        if (partner is null)
        {
            throw new ApiException(StatusCodes.Status404NotFound, "Партнер не найден");
        }

        await partnerRepo.DeleteAsync(partnerId, cancellationToken);
        return Ok(new { success = true, message = "Партнер удален" });
    }
}
