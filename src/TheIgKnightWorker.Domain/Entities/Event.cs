using static TheIgKnightWorker.Domain.Enums.Enums;

namespace TheIgKnightWorker.Domain.Entities;

public class Event
{
    public long Id { get; set; }
    public Guid EventId { get; set; }
    public EventType EventType { get; set; }
    public Guid? EntityId { get; set; }
    public string? EntityType { get; set; }
    public Guid? UserId { get; set; }
    public Guid? JobId { get; set; }
    public Guid? MessageId { get; set; }
    public Guid? CorrelationId { get; set; }
    public string Source { get; set; } = null!;
    public EventStatus Status { get; set; }
    public int AttemptNumber { get; set; }
    public string? Data { get; set; }
    public string? ErrorCode { get; set; }
    public string? ErrorMessage { get; set; }
    public DateTime CreatedAt { get; set; }
    public Guid? CreatedBy { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public Guid? UpdatedBy { get; set; }
    public bool IsDeleted { get; set; }
}