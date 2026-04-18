using System.Text.Json;

namespace Donately.Domain.Entities;

public class AuditLog
{
    public Guid Id { get; set; }

    public string EntityType { get; set; } = string.Empty;

    public Guid? EntityId { get; set; }

    public string Action { get; set; } = string.Empty;

    public JsonDocument? Data { get; set; }

    public Guid? PerformedById { get; set; }

    public ApplicationUser? PerformedBy { get; set; }

    public DateTime PerformedAt { get; set; }
}

