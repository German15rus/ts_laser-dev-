using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TsLaser.Crm.Api.Common;
using TsLaser.Crm.Api.Contracts;
using TsLaser.Crm.Api.Domain.Enums;
using TsLaser.Crm.Api.Infrastructure.Persistence;
using TsLaser.Crm.Api.Infrastructure.Services;

namespace TsLaser.Crm.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/bookings")]
public sealed class BookingsController(
    AppDbContext dbContext,
    BookingModerationService moderationService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<List<BookingListItemResponse>>> GetBookings(
        [FromQuery] string? status,
        CancellationToken cancellationToken = default)
    {
        var normalizedStatus = NormalizeStatusFilter(status);

        var query = dbContext.IntakeSubmissions
            .AsNoTracking()
            .Where(x => x.Status == normalizedStatus)
            .OrderByDescending(x => x.CreatedAt);

        var items = await query.ToListAsync(cancellationToken);
        return Ok(items.Select(x => x.ToListResponse()).ToList());
    }

    [HttpGet("{submissionId:int}")]
    public async Task<ActionResult<BookingDetailsResponse>> GetBooking(int submissionId, CancellationToken cancellationToken)
    {
        var submission = await dbContext.IntakeSubmissions.AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == submissionId, cancellationToken);

        if (submission is null)
        {
            throw new ApiException(StatusCodes.Status404NotFound, "Заявка не найдена");
        }

        return Ok(submission.ToDetailsResponse());
    }

    [HttpPost("{submissionId:int}/approve")]
    public async Task<ActionResult<BookingModerationResponse>> ApproveBooking(int submissionId, CancellationToken cancellationToken)
    {
        var reviewer = GetReviewer();
        var submission = await moderationService.ApproveAsync(submissionId, reviewer, cancellationToken);

        return Ok(new BookingModerationResponse(
            true,
            "Заявка одобрена",
            submission.Id,
            submission.Status,
            submission.ApprovedClientId,
            submission.ApprovedTattooId));
    }

    [HttpPost("{submissionId:int}/reject")]
    public async Task<ActionResult<BookingModerationResponse>> RejectBooking(
        int submissionId,
        [FromBody] BookingRejectRequest? request,
        CancellationToken cancellationToken)
    {
        var reviewer = GetReviewer();
        var submission = await moderationService.RejectAsync(submissionId, reviewer, request?.RejectionReason, cancellationToken);

        return Ok(new BookingModerationResponse(
            true,
            "Заявка отклонена",
            submission.Id,
            submission.Status,
            submission.ApprovedClientId,
            submission.ApprovedTattooId));
    }

    private static string NormalizeStatusFilter(string? status)
    {
        if (string.IsNullOrWhiteSpace(status))
        {
            return IntakeSubmissionStatus.Pending;
        }

        if (!IntakeSubmissionStatus.IsAllowed(status))
        {
            throw new ApiException(StatusCodes.Status400BadRequest, "Invalid status filter");
        }

        return status.Trim().ToLowerInvariant();
    }

    private string GetReviewer()
    {
        return User.Identity?.Name is { Length: > 0 } name ? name : "admin";
    }
}
