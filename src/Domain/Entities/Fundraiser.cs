namespace Donately.Domain.Entities;

public class Fundraiser
{
    public Guid Id { get; set; }

    public string Title { get; set; } = string.Empty;

    public string Slug { get; set; } = string.Empty;

    public string? ShortDescription { get; set; }

    public string? Description { get; set; }

    public FundraiserCategory Category { get; set; } = FundraiserCategory.Support;

    public decimal GoalAmount { get; set; }

    public decimal CurrentAmount { get; set; }

    public string Currency { get; set; } = "UAH";

    public bool IsUrgent { get; set; }

    public Guid CreatedById { get; set; }

    public ApplicationUser CreatedBy { get; set; } = null!;

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public bool IsActive { get; set; } = true;

    public bool IsDeleted { get; set; }

    // Designed as an external file reference (Blob/S3), not a local binary payload.
    public Guid? CoverImageId { get; set; }

    public byte[] RowVersion { get; set; } = Array.Empty<byte>();

    public ICollection<Donation> Donations { get; set; } = new List<Donation>();
}

