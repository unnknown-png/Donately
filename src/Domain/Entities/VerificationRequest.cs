namespace Donately.Domain.Entities;

public class VerificationRequest
{
    public Guid Id { get; set; }

    public Guid UserId { get; set; }

    public ApplicationUser User { get; set; } = null!;

    public Guid AttachmentId { get; set; }

    public Attachment Attachment { get; set; } = null!;

    public VerificationRequestStatus Status { get; set; } = VerificationRequestStatus.Pending;

    public Guid? ReviewedById { get; set; }

    public ApplicationUser? ReviewedBy { get; set; }

    public DateTime? ReviewedAt { get; set; }

    public string? Notes { get; set; }

    public DateTime CreatedAt { get; set; }
}

