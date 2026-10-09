namespace GameStore.Data.Models;

public class OutboxMessage
{
    public Guid Id { get; set; }

    public required string MessageType { get; set; }

    public required string QueueName { get; set; }

    public required string Payload { get; set; }

    public string? MessageId { get; set; }

    public string? CorrelationId { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? ProcessedAt { get; set; }

    public int RetryCount { get; set; }

    public string? LastError { get; set; }
}
