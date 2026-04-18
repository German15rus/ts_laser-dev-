using TsLaser.Crm.Api.Common;
using TsLaser.Crm.Api.Domain.Entities;

namespace TsLaser.Crm.Api.Contracts;

public static class MappingExtensions
{
    public static PartnerResponse ToResponse(this Partner partner) => new()
    {
        Id = partner.Id,
        Name = partner.Name,
        Contacts = partner.Contacts,
        Type = partner.Type,
        Terms = partner.Terms,
        Comment = partner.Comment,
        CreatedAt = partner.CreatedAt,
        UpdatedAt = partner.UpdatedAt,
    };

    public static ClientResponse ToResponse(this Client client, string? partnerName = null) => new()
    {
        Id = client.Id,
        Name = client.Name,
        Phone = client.Phone,
        BirthDate = client.BirthDate,
        Age = client.Age,
        Gender = client.Gender,
        Address = client.Address,
        ReferralPartnerId = client.ReferralPartnerId,
        ReferralCustom = client.ReferralCustom,
        Status = client.Status,
        StoppedReason = client.StoppedReason,
        ReferralPartnerName = partnerName,
        CreatedAt = client.CreatedAt,
        UpdatedAt = client.UpdatedAt,
    };

    public static ClientListResponse ToListResponse(this Client client) => new()
    {
        Id = client.Id,
        Name = client.Name,
        Phone = client.Phone,
        Address = client.Address,
        Status = client.Status,
        CreatedAt = client.CreatedAt,
    };

    public static TattooResponse ToResponse(this Tattoo tattoo) => new()
    {
        Id = tattoo.Id,
        ClientId = tattoo.ClientId,
        Name = tattoo.Name,
        RemovalZone = tattoo.RemovalZone,
        CorrectionsCount = tattoo.CorrectionsCount,
        LastPigmentDate = tattoo.LastPigmentDate,
        LastLaserDate = tattoo.LastLaserDate,
        NoLaserBefore = tattoo.NoLaserBefore,
        PreviousRemovalPlace = tattoo.PreviousRemovalPlace,
        DesiredResult = tattoo.DesiredResult,
        CreatedAt = tattoo.CreatedAt,
        UpdatedAt = tattoo.UpdatedAt,
    };

    public static LaserSessionResponse ToResponse(this LaserSession session) => new()
    {
        Id = session.Id,
        ClientId = session.ClientId,
        TattooId = session.TattooId,
        TattooName = session.TattooName,
        SessionNumber = session.SessionNumber,
        SubSession = session.SubSession,
        Wavelength = session.Wavelength,
        Diameter = session.Diameter,
        Density = session.Density,
        Hertz = session.Hertz,
        FlashesCount = session.FlashesCount,
        SessionDate = session.SessionDate,
        BreakPeriod = session.BreakPeriod,
        Comment = session.Comment,
        CreatedAt = session.CreatedAt,
        UpdatedAt = session.UpdatedAt,
    };

    public static BookingListItemResponse ToListResponse(this IntakeSubmission submission) => new()
    {
        Id = submission.Id,
        FullName = submission.FullName,
        Phone = submission.Phone,
        TattooType = submission.TattooType,
        ReferralSource = submission.ReferralSource,
        Status = submission.Status,
        CreatedAt = submission.CreatedAt,
        ReviewedAt = submission.ReviewedAt,
    };

    public static BookingDetailsResponse ToDetailsResponse(this IntakeSubmission submission) => new()
    {
        Id = submission.Id,
        FullName = submission.FullName,
        Gender = submission.Gender,
        Phone = submission.Phone,
        BirthDate = submission.BirthDate,
        Age = InputCleaner.CalculateAge(submission.BirthDate),
        Address = submission.Address,
        ReferralSource = submission.ReferralSource,
        TattooType = submission.TattooType,
        TattooAge = submission.TattooAge,
        CorrectionsInfo = submission.CorrectionsInfo,
        PreviousRemovalInfo = submission.PreviousRemovalInfo,
        PreviousRemovalWhere = submission.PreviousRemovalWhere,
        DesiredResult = submission.DesiredResult,
        Status = submission.Status,
        Source = submission.Source,
        IsNewClient = submission.IsNewClient,
        RejectionReason = submission.RejectionReason,
        ReviewedBy = submission.ReviewedBy,
        ReviewedAt = submission.ReviewedAt,
        ApprovedClientId = submission.ApprovedClientId,
        ApprovedTattooId = submission.ApprovedTattooId,
        CreatedAt = submission.CreatedAt,
        UpdatedAt = submission.UpdatedAt,
    };
}
