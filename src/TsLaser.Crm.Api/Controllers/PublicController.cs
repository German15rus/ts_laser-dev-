using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using TsLaser.Crm.Api.Common;
using TsLaser.Crm.Api.Contracts;
using TsLaser.Crm.Api.Domain.Entities;
using TsLaser.Crm.Api.Domain.Enums;
using TsLaser.Crm.Api.Infrastructure.Repositories;

namespace TsLaser.Crm.Api.Controllers;

[ApiController]
[Route("api/public")]
public sealed class PublicController(IntakeSubmissionRepository submissionRepo) : ControllerBase
{
    [AllowAnonymous]
    [EnableRateLimiting("booking")]
    [HttpPost("booking")]
    public async Task<ActionResult<PublicBookingResponse>> CreatePublicBooking([FromBody] PublicBookingCreateRequest booking, CancellationToken cancellationToken)
    {
        if (!string.IsNullOrWhiteSpace(booking.Website))
        {
            throw new ApiException(StatusCodes.Status400BadRequest, "Invalid request");
        }

        if (!booking.ConsentPersonalData)
        {
            throw new ApiException(StatusCodes.Status400BadRequest, "Consent is required");
        }

        if (booking.FormStartedAt.HasValue)
        {
            var elapsedMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() - booking.FormStartedAt.Value;
            if (elapsedMs < 2000)
            {
                throw new ApiException(StatusCodes.Status400BadRequest, "Form submitted too fast");
            }
        }

        var fullName = InputCleaner.CleanRequired(booking.FullName, 255);
        var gender = InputCleaner.CleanRequired(booking.Gender, 50);
        var normalizedPhone = InputCleaner.NormalizePhone(booking.Phone);
        var address = InputCleaner.CleanRequired(booking.Address, 500);
        var referralSource = InputCleaner.CleanRequired(booking.ReferralSource, 255);
        var tattooType = InputCleaner.CleanRequired(booking.TattooType, 255);
        var tattooAge = InputCleaner.CleanRequired(booking.TattooAge, 255);
        var correctionsInfo = InputCleaner.CleanRequired(booking.CorrectionsInfo, 500);
        var previousRemovalInfo = InputCleaner.CleanRequired(booking.PreviousRemovalInfo, 500);
        var previousRemovalWhere = InputCleaner.CleanRequired(booking.PreviousRemovalWhere, 500);
        var desiredResult = InputCleaner.CleanRequired(booking.DesiredResult, 500);

        var payloadDict = new Dictionary<string, object?>
        {
            ["full_name"] = booking.FullName,
            ["gender"] = booking.Gender,
            ["phone"] = booking.Phone,
            ["phone_normalized"] = normalizedPhone,
            ["birth_date"] = booking.BirthDate,
            ["address"] = booking.Address,
            ["referral_source"] = booking.ReferralSource,
            ["tattoo_type"] = booking.TattooType,
            ["tattoo_age"] = booking.TattooAge,
            ["corrections_info"] = booking.CorrectionsInfo,
            ["previous_removal_info"] = booking.PreviousRemovalInfo,
            ["previous_removal_where"] = booking.PreviousRemovalWhere,
            ["desired_result"] = booking.DesiredResult,
            ["consent_personal_data"] = booking.ConsentPersonalData,
            ["form_started_at"] = booking.FormStartedAt,
            ["website"] = booking.Website,
        };

        var submission = new IntakeSubmission
        {
            FullName = fullName,
            Gender = gender,
            Phone = normalizedPhone,
            BirthDate = booking.BirthDate,
            Address = address,
            ReferralSource = referralSource,
            TattooType = tattooType,
            TattooAge = tattooAge,
            CorrectionsInfo = correctionsInfo,
            PreviousRemovalInfo = previousRemovalInfo,
            PreviousRemovalWhere = previousRemovalWhere,
            DesiredResult = desiredResult,
            Source = "landing",
            Status = IntakeSubmissionStatus.Pending,
            IsNewClient = false,
            RawPayload = JsonSerializer.Serialize(payloadDict),
        };

        await submissionRepo.CreateAsync(submission, cancellationToken);

        return Ok(new PublicBookingResponse(true, "Booking request saved", submission.Id));
    }
}
