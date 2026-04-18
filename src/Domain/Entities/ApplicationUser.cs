using Microsoft.AspNetCore.Identity;

namespace Donately.Domain.Entities;

public class ApplicationUser : IdentityUser<Guid>
{
    public string? FullName { get; set; }

    public bool IsVerified { get; set; }

    public VerificationStatus VerificationStatus { get; set; } = VerificationStatus.None;

    public string? ProfileImagePath { get; set; }

    public string? Bio { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public byte[] RowVersion { get; set; } = Array.Empty<byte>();
}