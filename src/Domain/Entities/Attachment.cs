namespace Donately.Domain.Entities;

public class Attachment
{
    public Guid Id { get; set; }

    public string OwnerType { get; set; } = string.Empty;

    public Guid OwnerId { get; set; }

    public string Url { get; set; } = string.Empty;

    public string? ContentType { get; set; }

    public string? Purpose { get; set; }

    public DateTime CreatedAt { get; set; }
}

