using TsLaser.Crm.Api.Common;
using TsLaser.Crm.Api.Domain.Entities;
using TsLaser.Crm.Api.Domain.Enums;
using TsLaser.Crm.Api.Infrastructure.Repositories;

namespace TsLaser.Crm.Api.Infrastructure.Services;

public sealed class BookingModerationService(
    ClientRepository clientRepo,
    TattooRepository tattooRepo,
    IntakeSubmissionRepository submissionRepo)
{
    public async Task<IntakeSubmission> ApproveAsync(int submissionId, string reviewer, CancellationToken cancellationToken = default)
    {
        var submission = await submissionRepo.GetByIdAsync(submissionId, cancellationToken);
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

        var client = await clientRepo.GetByPhoneAsync(normalizedPhone, cancellationToken);
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
            await clientRepo.CreateAsync(client, cancellationToken);
        }
        else
        {
            var changed = false;

            if (InputCleaner.IsNotFilled(client.Name))
            {
                client.Name = fullName;
                changed = true;
            }

            if (client.BirthDate is null && submission.BirthDate is not null)
            {
                client.BirthDate = submission.BirthDate;
                client.Age = InputCleaner.CalculateAge(submission.BirthDate);
                changed = true;
            }

            if (InputCleaner.IsNotFilled(client.Address))
            {
                client.Address = address;
                changed = true;
            }

            if (InputCleaner.IsNotFilled(client.ReferralCustom))
            {
                client.ReferralCustom = referralSource;
                changed = true;
            }

            if (changed)
            {
                await clientRepo.UpdateAsync(client, cancellationToken);
            }
        }

        var tattoo = await tattooRepo.GetByClientIdAndNameAsync(client.Id, tattooType, cancellationToken);

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
            await tattooRepo.CreateAsync(tattoo, cancellationToken);
        }
        else
        {
            var changed = false;

            if (InputCleaner.IsNotFilled(tattoo.CorrectionsCount))
            {
                tattoo.CorrectionsCount = correctionsInfo;
                changed = true;
            }

            if (InputCleaner.IsNotFilled(tattoo.PreviousRemovalPlace))
            {
                tattoo.PreviousRemovalPlace = previousRemovalWhere;
                changed = true;
            }

            if (InputCleaner.IsNotFilled(tattoo.DesiredResult))
            {
                tattoo.DesiredResult = desiredResult;
                changed = true;
            }

            if (changed)
            {
                await tattooRepo.UpdateAsync(tattoo, cancellationToken);
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

        await submissionRepo.UpdateAsync(submission, cancellationToken);

        return submission;
    }

    public async Task<IntakeSubmission> RejectAsync(
        int submissionId,
        string reviewer,
        string? rejectionReason,
        CancellationToken cancellationToken = default)
    {
        var submission = await submissionRepo.GetByIdAsync(submissionId, cancellationToken);
        if (submission is null)
        {
            throw new ApiException(StatusCodes.Status404NotFound, "Заявка не найдена");
        }

        EnsurePending(submission);

        submission.Status = IntakeSubmissionStatus.Rejected;
        submission.ReviewedAt = DateTime.UtcNow;
        submission.ReviewedBy = reviewer;
        submission.RejectionReason = NormalizeRejectionReason(rejectionReason);

        await submissionRepo.UpdateAsync(submission, cancellationToken);
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
        if (string.IsNullOrWhiteSpace(reason)) return null;
        var trimmed = reason.Trim();
        return trimmed.Length <= 1000 ? trimmed : trimmed[..1000];
    }
}
