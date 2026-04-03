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
[Route("api/partners")]
public sealed class PartnersController(AppDbContext dbContext) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<List<PartnerResponse>>> GetPartners(
        [FromQuery] string? search,
        [FromQuery(Name = "type_filter")] string? typeFilter,
        [FromQuery(Name = "sort_by")] string sortBy = "name",
        [FromQuery(Name = "sort_order")] string sortOrder = "asc",
        CancellationToken cancellationToken = default)
    {
        IQueryable<Partner> query = dbContext.Partners.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(typeFilter))
        {
            query = query.Where(x => x.Type == typeFilter);
        }

        query = (sortBy.ToLowerInvariant(), sortOrder.ToLowerInvariant()) switch
        {
            ("created_at", "desc") => query.OrderByDescending(x => x.CreatedAt),
            ("created_at", _) => query.OrderBy(x => x.CreatedAt),
            (_, "desc") => query.OrderByDescending(x => x.Name),
            _ => query.OrderBy(x => x.Name),
        };

        var items = await query.ToListAsync(cancellationToken);

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

        dbContext.Partners.Add(partner);
        await dbContext.SaveChangesAsync(cancellationToken);

        return CreatedAtAction(nameof(GetPartner), new { partnerId = partner.Id }, partner.ToResponse());
    }

    [HttpGet("{partnerId:int}")]
    public async Task<ActionResult<PartnerResponse>> GetPartner(int partnerId, CancellationToken cancellationToken)
    {
        var partner = await dbContext.Partners.AsNoTracking().FirstOrDefaultAsync(x => x.Id == partnerId, cancellationToken);
        if (partner is null)
        {
            throw new ApiException(StatusCodes.Status404NotFound, "Партнер не найден");
        }

        return Ok(partner.ToResponse());
    }

    [HttpPut("{partnerId:int}")]
    public async Task<ActionResult<PartnerResponse>> UpdatePartner(int partnerId, [FromBody] PartnerUpdateRequest request, CancellationToken cancellationToken)
    {
        var partner = await dbContext.Partners.FirstOrDefaultAsync(x => x.Id == partnerId, cancellationToken);
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

        await dbContext.SaveChangesAsync(cancellationToken);
        return Ok(partner.ToResponse());
    }

    [HttpDelete("{partnerId:int}")]
    public async Task<IActionResult> DeletePartner(int partnerId, CancellationToken cancellationToken)
    {
        var partner = await dbContext.Partners.FirstOrDefaultAsync(x => x.Id == partnerId, cancellationToken);
        if (partner is null)
        {
            throw new ApiException(StatusCodes.Status404NotFound, "Партнер не найден");
        }

        dbContext.Partners.Remove(partner);
        await dbContext.SaveChangesAsync(cancellationToken);

        return Ok(new { success = true, message = "Партнер удален" });
    }
}
