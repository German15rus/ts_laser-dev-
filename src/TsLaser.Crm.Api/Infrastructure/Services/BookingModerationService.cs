using Microsoft.EntityFrameworkCore;
using TsLaser.Crm.Api.Common;
using TsLaser.Crm.Api.Domain.Entities;
using TsLaser.Crm.Api.Domain.Enums;
using TsLaser.Crm.Api.Infrastructure.Persistence;

namespace TsLaser.Crm.Api.Infrastructure.Services;

public sealed class BookingModerationService(AppDbContext dbContext, FirestoreService firestoreService)
{
    public async Task<IntakeSubmission> ApproveAsync(int submissionId, string reviewer, CancellationToken cancellationToken = default)
    {
        var submission = await dbContext.IntakeSubmissions.FirstOrDefaultAsync(x => x.Id == submissionId, cancellationToken);
        if (submission is null)
        {
            throw new ApiException(StatusCodes.Status404NotFound, "Заявка не найдена");
        }

        EnsurePending(submission);

        var fullName = InputCleaner.CleanRequired(submission.FullName, 255);
        var normalizedPhone = InputCleaner.NormalizePhone(submission.Phone);
        var address = InputCleaner.CleanRequired(submission.Address, 500);
        var referralSource = InputCleaner.CleanRequired(submission.ReferralSource, 255);
        var tattooType = InputCleaner.CleanRequired(submission.TattooType, 255);
        var correctionsInfo = InputCleaner.CleanRequired(submission.CorrectionsInfo, 500);
        var previousRemovalInfo = InputCleaner.CleanRequired(submission.PreviousRemovalInfo, 500);
        var previousRemovalWhere = InputCleaner.CleanRequired(submission.PreviousRemovalWhere, 500);
        var desiredResult = InputCleaner.CleanRequired(submission.DesiredResult, 500);

        await using var tx = await dbContext.Database.BeginTransactionAsync(cancellationToken);

        var client = await dbContext.Clients.FirstOrDefaultAsync(x => x.Phone == normalizedPhone, cancellationToken);
        var isNewClient = client is null;

        if (client is null)
        {
            client = new Client
            {
                Name = fullName,
                Phone = normalizedPhone,
                BirthDate = submission.BirthDate,
                Age = InputCleaner.CalculateAge(submission.BirthDate),
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

            if (client.BirthDate is null && submission.BirthDate is not null)
            {
                client.BirthDate = submission.BirthDate;
                client.Age = InputCleaner.CalculateAge(submission.BirthDate);
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

        submission.FullName = fullName;
        submission.Phone = normalizedPhone;
        submission.Address = address;
        submission.ReferralSource = referralSource;
        submission.TattooType = tattooType;
        submission.CorrectionsInfo = correctionsInfo;
        submission.PreviousRemovalInfo = previousRemovalInfo;
        submission.PreviousRemovalWhere = previousRemovalWhere;
        submission.DesiredResult = desiredResult;

        submission.Status = IntakeSubmissionStatus.Approved;
        submission.IsNewClient = isNewClient;
        submission.ReviewedAt = DateTime.UtcNow;
        submission.ReviewedBy = reviewer;
        submission.RejectionReason = null;
        submission.ClientId = client.Id;
        submission.TattooId = tattoo.Id;
        submission.ApprovedClientId = client.Id;
        submission.ApprovedTattooId = tattoo.Id;

        await dbContext.SaveChangesAsync(cancellationToken);
        await tx.CommitAsync(cancellationToken);
        await firestoreService.DeleteSubmissionAsync(submission.Id, cancellationToken);

        return submission;
    }

    public async Task<IntakeSubmission> RejectAsync(
        int submissionId,
        string reviewer,
        string? rejectionReason,
        CancellationToken cancellationToken = default)
    {
        var submission = await dbContext.IntakeSubmissions.FirstOrDefaultAsync(x => x.Id == submissionId, cancellationToken);
        if (submission is null)
        {
            throw new ApiException(StatusCodes.Status404NotFound, "Заявка не найдена");
        }

        EnsurePending(submission);

        submission.Status = IntakeSubmissionStatus.Rejected;
        submission.ReviewedAt = DateTime.UtcNow;
        submission.ReviewedBy = reviewer;
        submission.RejectionReason = NormalizeRejectionReason(rejectionReason);

        await dbContext.SaveChangesAsync(cancellationToken);
        await firestoreService.DeleteSubmissionAsync(submission.Id, cancellationToken);
        return submission;
    }

    private static void EnsurePending(IntakeSubmission submission)
    {
        if (string.Equals(submission.Status, IntakeSubmissionStatus.Pending, StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        var message = string.Equals(submission.Status, IntakeSubmissionStatus.Approved, StringComparison.OrdinalIgnoreCase)
            ? "Заявка уже одобрена"
            : "Заявка уже отклонена";

        throw new ApiException(StatusCodes.Status409Conflict, message);
    }

    private static string? NormalizeRejectionReason(string? reason)
    {
        if (string.IsNullOrWhiteSpace(reason))
        {
            return null;
        }

        var trimmed = reason.Trim();
        return trimmed.Length <= 1000 ? trimmed : trimmed[..1000];
    }
}
