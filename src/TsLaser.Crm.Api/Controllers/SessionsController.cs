using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TsLaser.Crm.Api.Common;
using TsLaser.Crm.Api.Contracts;
using TsLaser.Crm.Api.Domain.Entities;
using TsLaser.Crm.Api.Infrastructure.Repositories;

namespace TsLaser.Crm.Api.Controllers;

[Authorize]
[ApiController]
public sealed class SessionsController(ClientRepository clientRepo, LaserSessionRepository sessionRepo) : ControllerBase
{
    [HttpGet("api/clients/{clientId:int}/sessions")]
    public async Task<ActionResult<LaserSessionsListResponse>> GetClientSessions(
        int clientId,
        [FromQuery(Name = "tattoo_filter")] string? tattooFilter,
        [FromQuery(Name = "sort_by")] string sortBy = "session_date",
        [FromQuery(Name = "sort_order")] string sortOrder = "asc",
        CancellationToken cancellationToken = default)
    {
        var client = await clientRepo.GetByIdAsync(clientId, cancellationToken);
        if (client is null)
        {
            throw new ApiException(StatusCodes.Status404NotFound, "Клиент не найден");
        }

        var sessions = await sessionRepo.GetByClientIdAsync(clientId, tattooFilter, sortBy, sortOrder, cancellationToken);
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
        var clientExists = await clientRepo.ExistsAsync(clientId, cancellationToken);
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

        await sessionRepo.CreateAsync(session, cancellationToken);
        return CreatedAtAction(nameof(GetSession), new { sessionId = session.Id }, session.ToResponse());
    }

    [HttpGet("api/sessions/{sessionId:int}")]
    public async Task<ActionResult<LaserSessionResponse>> GetSession(int sessionId, CancellationToken cancellationToken)
    {
        var session = await sessionRepo.GetByIdAsync(sessionId, cancellationToken);
        if (session is null)
        {
            throw new ApiException(StatusCodes.Status404NotFound, "Сеанс не найден");
        }

        return Ok(session.ToResponse());
    }

    [HttpPut("api/sessions/{sessionId:int}")]
    public async Task<ActionResult<LaserSessionResponse>> UpdateSession(int sessionId, [FromBody] LaserSessionUpdateRequest request, CancellationToken cancellationToken)
    {
        var session = await sessionRepo.GetByIdAsync(sessionId, cancellationToken);
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

        await sessionRepo.UpdateAsync(session, cancellationToken);
        return Ok(session.ToResponse());
    }

    [HttpDelete("api/sessions/{sessionId:int}")]
    public async Task<IActionResult> DeleteSession(int sessionId, CancellationToken cancellationToken)
    {
        var session = await sessionRepo.GetByIdAsync(sessionId, cancellationToken);
        if (session is null)
        {
            throw new ApiException(StatusCodes.Status404NotFound, "Сеанс не найден");
        }

        await sessionRepo.DeleteAsync(sessionId, cancellationToken);
        return Ok(new { success = true, message = "Сеанс удален" });
    }
}
