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
public sealed class SessionsController(AppDbContext dbContext) : ControllerBase
{
    [HttpGet("api/clients/{clientId:int}/sessions")]
    public async Task<ActionResult<LaserSessionsListResponse>> GetClientSessions(
        int clientId,
        [FromQuery(Name = "tattoo_filter")] string? tattooFilter,
        [FromQuery(Name = "sort_by")] string sortBy = "session_date",
        [FromQuery(Name = "sort_order")] string sortOrder = "asc",
        CancellationToken cancellationToken = default)
    {
        var client = await dbContext.Clients.AsNoTracking().FirstOrDefaultAsync(x => x.Id == clientId, cancellationToken);
        if (client is null)
        {
            throw new ApiException(StatusCodes.Status404NotFound, "Клиент не найден");
        }

        IQueryable<LaserSession> query = dbContext.LaserSessions.AsNoTracking().Where(x => x.ClientId == clientId);

        if (!string.IsNullOrWhiteSpace(tattooFilter))
        {
            query = query.Where(x => x.TattooName == tattooFilter);
        }

        query = (sortBy.ToLowerInvariant(), sortOrder.ToLowerInvariant()) switch
        {
            ("tattoo_name", "desc") => query.OrderByDescending(x => x.TattooName),
            ("tattoo_name", _) => query.OrderBy(x => x.TattooName),
            ("session_number", "desc") => query.OrderByDescending(x => x.SessionNumber),
            ("session_number", _) => query.OrderBy(x => x.SessionNumber),
            (_, "desc") => query.OrderByDescending(x => x.SessionDate),
            _ => query.OrderBy(x => x.SessionDate),
        };

        var sessions = await query.ToListAsync(cancellationToken);
        var totalFlashes = sessions.Sum(x => x.FlashesCount ?? 0);

        return Ok(new LaserSessionsListResponse
        {
            Sessions = sessions.Select(x => x.ToResponse()).ToList(),
            TotalFlashes = totalFlashes,
            ClientId = client.Id,
            ClientName = client.Name,
        });
    }

    [HttpPost("api/clients/{clientId:int}/sessions")]
    public async Task<ActionResult<LaserSessionResponse>> CreateSession(int clientId, [FromBody] LaserSessionCreateRequest request, CancellationToken cancellationToken)
    {
        var clientExists = await dbContext.Clients.AnyAsync(x => x.Id == clientId, cancellationToken);
        if (!clientExists)
        {
            throw new ApiException(StatusCodes.Status404NotFound, "Клиент не найден");
        }

        var session = new LaserSession
        {
            ClientId = clientId,
            TattooId = request.TattooId,
            TattooName = request.TattooName,
            SessionNumber = request.SessionNumber,
            SubSession = request.SubSession,
            Wavelength = request.Wavelength,
            Diameter = request.Diameter,
            Density = request.Density,
            Hertz = request.Hertz,
            FlashesCount = request.FlashesCount,
            SessionDate = request.SessionDate,
            BreakPeriod = request.BreakPeriod,
            Comment = request.Comment,
        };

        dbContext.LaserSessions.Add(session);
        await dbContext.SaveChangesAsync(cancellationToken);

        return CreatedAtAction(nameof(GetSession), new { sessionId = session.Id }, session.ToResponse());
    }

    [HttpGet("api/sessions/{sessionId:int}")]
    public async Task<ActionResult<LaserSessionResponse>> GetSession(int sessionId, CancellationToken cancellationToken)
    {
        var session = await dbContext.LaserSessions.AsNoTracking().FirstOrDefaultAsync(x => x.Id == sessionId, cancellationToken);
        if (session is null)
        {
            throw new ApiException(StatusCodes.Status404NotFound, "Сеанс не найден");
        }

        return Ok(session.ToResponse());
    }

    [HttpPut("api/sessions/{sessionId:int}")]
    public async Task<ActionResult<LaserSessionResponse>> UpdateSession(int sessionId, [FromBody] LaserSessionUpdateRequest request, CancellationToken cancellationToken)
    {
        var session = await dbContext.LaserSessions.FirstOrDefaultAsync(x => x.Id == sessionId, cancellationToken);
        if (session is null)
        {
            throw new ApiException(StatusCodes.Status404NotFound, "Сеанс не найден");
        }

        session.TattooId = request.TattooId;
        session.TattooName = request.TattooName;
        session.SessionNumber = request.SessionNumber;
        session.SubSession = request.SubSession;
        session.Wavelength = request.Wavelength;
        session.Diameter = request.Diameter;
        session.Density = request.Density;
        session.Hertz = request.Hertz;
        session.FlashesCount = request.FlashesCount;
        session.SessionDate = request.SessionDate;
        session.BreakPeriod = request.BreakPeriod;
        session.Comment = request.Comment;

        await dbContext.SaveChangesAsync(cancellationToken);

        return Ok(session.ToResponse());
    }

    [HttpDelete("api/sessions/{sessionId:int}")]
    public async Task<IActionResult> DeleteSession(int sessionId, CancellationToken cancellationToken)
    {
        var session = await dbContext.LaserSessions.FirstOrDefaultAsync(x => x.Id == sessionId, cancellationToken);
        if (session is null)
        {
            throw new ApiException(StatusCodes.Status404NotFound, "Сеанс не найден");
        }

        dbContext.LaserSessions.Remove(session);
        await dbContext.SaveChangesAsync(cancellationToken);

        return Ok(new { success = true, message = "Сеанс удален" });
    }
}
