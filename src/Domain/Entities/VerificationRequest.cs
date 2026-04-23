namespace Donately.Domain.Entities;

public class VerificationRequest
{
    public Guid Id { get; set; }

    public Guid UserId { get; set; }

    public ApplicationUser User { get; set; } = null!;

    public string Email { get; set; } = string.Empty;

    public string? EmailConfirmationTokenHash { get; set; }

    public DateTime? EmailConfirmationTokenExpiresAt { get; set; }

    public DateTime? EmailConfirmedAt { get; set; }

    public DateTime? PhoneNumberConfirmedAt { get; set; }

    public Guid? AttachmentId { get; set; }

    public Attachment? Attachment { get; set; }

    public VerificationRequestStatus Status { get; set; } = VerificationRequestStatus.NotStarted;

    public Guid? ReviewedById { get; set; }

    public ApplicationUser? ReviewedBy { get; set; }

    public DateTime? ReviewedAt { get; set; }

    public string? Notes { get; set; }

    public DateTime CreatedAt { get; set; }
}

