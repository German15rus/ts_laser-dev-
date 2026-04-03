namespace TsLaser.Crm.Api.Domain.Entities;

public sealed class Partner : TimestampedEntity
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string? Contacts { get; set; }

    public string? Type { get; set; }

    public string? Terms { get; set; }

    public string? Comment { get; set; }

    public ICollection<Client> ReferredClients { get; set; } = new List<Client>();
}
