using System.ComponentModel.DataAnnotations.Schema;

namespace Domain.Entities;

[Table("OutboxMessages", Schema = "Messaging")]
public class OutboxMessage
{
    public Guid Id { get; set; }

    public string EventType { get; set; } = string.Empty;
    public string EventPayload { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ProcessedAt { get; set; }


    public string? Error { get; set; }
    public int RetryCount { get; set; }
    public DateTime? NextAttemptAt { get; set; }
}
