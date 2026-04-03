using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using TsLaser.Crm.Api.Common;
using TsLaser.Crm.Api.Contracts;
using TsLaser.Crm.Api.Domain.Entities;
using TsLaser.Crm.Api.Infrastructure.Persistence;

namespace TsLaser.Crm.Api.Controllers;

[ApiController]
[Route("api/public")]
public sealed class PublicController(AppDbContext dbContext) : ControllerBase
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
        var normalizedPhone = InputCleaner.NormalizePhone(booking.Phone);
        var address = InputCleaner.CleanRequired(booking.Address, 500);
        var referralSource = InputCleaner.CleanRequired(booking.ReferralSource, 255);
        var tattooType = InputCleaner.CleanRequired(booking.TattooType, 255);
        var tattooAge = InputCleaner.CleanRequired(booking.TattooAge, 255);
        var correctionsInfo = InputCleaner.CleanRequired(booking.CorrectionsInfo, 500);
        var previousRemovalInfo = InputCleaner.CleanRequired(booking.PreviousRemovalInfo, 500);
        var previousRemovalWhere = InputCleaner.CleanRequired(booking.PreviousRemovalWhere, 500);
        var desiredResult = InputCleaner.CleanRequired(booking.DesiredResult, 500);

        var client = await dbContext.Clients.FirstOrDefaultAsync(x => x.Phone == normalizedPhone, cancellationToken);
        var isNewClient = client is null;

        if (client is null)
        {
            client = new Client
            {
                Name = fullName,
                Phone = normalizedPhone,
                BirthDate = booking.BirthDate,
                Address = address,
                ReferralCustom = referralSource,
                Status = "active",
            };

            dbContext.Clients.Add(client);
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        else
        {
            if (InputCleaner.IsNotFilled(client.Name))
            {
                client.Name = fullName;
            }

            if (client.BirthDate is null)
            {
                client.BirthDate = booking.BirthDate;
            }

            if (InputCleaner.IsNotFilled(client.Address))
            {
                client.Address = address;
            }

            if (InputCleaner.IsNotFilled(client.ReferralCustom))
            {
                client.ReferralCustom = referralSource;
            }
        }

        var tattoo = await dbContext.Tattoos.FirstOrDefaultAsync(
            x => x.ClientId == client.Id && x.Name == tattooType,
            cancellationToken);

        if (tattoo is null)
        {
            tattoo = new Tattoo
            {
                ClientId = client.Id,
                Name = tattooType,
                CorrectionsCount = correctionsInfo,
                NoLaserBefore = InputCleaner.IsNegativeAnswer(previousRemovalInfo),
                PreviousRemovalPlace = previousRemovalWhere,
                DesiredResult = desiredResult,
            };

            dbContext.Tattoos.Add(tattoo);
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        else
        {
            if (InputCleaner.IsNotFilled(tattoo.CorrectionsCount))
            {
                tattoo.CorrectionsCount = correctionsInfo;
            }

            if (InputCleaner.IsNotFilled(tattoo.PreviousRemovalPlace))
            {
                tattoo.PreviousRemovalPlace = previousRemovalWhere;
            }

            if (InputCleaner.IsNotFilled(tattoo.DesiredResult))
            {
                tattoo.DesiredResult = desiredResult;
            }
        }

        var payloadDict = new Dictionary<string, object?>
        {
            ["full_name"] = booking.FullName,
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

        dbContext.IntakeSubmissions.Add(new IntakeSubmission
        {
            ClientId = client.Id,
            TattooId = tattoo.Id,
            FullName = fullName,
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
            IsNewClient = isNewClient,
            RawPayload = JsonSerializer.Serialize(payloadDict),
        });

        await dbContext.SaveChangesAsync(cancellationToken);

        return Ok(new PublicBookingResponse(true, "Booking request saved", client.Id, isNewClient));
    }
}
