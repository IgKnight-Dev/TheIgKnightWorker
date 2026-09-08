namespace TheIgKnightWorker.Infrastructure.QueryConstants;

internal class EventSql
{
    public const string CreateEvent = @"
        INSERT INTO [Event]
        (
            EventId,
            EventType,
            EntityId,
            EntityType,
            UserId,
            JobId,
            MessageId,
            CorrelationId,
            Source,
            Status,
            AttemptNumber,
            Data,
            ErrorCode,
            ErrorMessage,
            CreatedAt,
            CreatedBy,
            UpdatedAt,
            UpdatedBy,
            IsDeleted
        )
        VALUES
        (
            @EventId,
            @EventType,
            @EntityId,
            @EntityType,
            @UserId,
            @JobId,
            @MessageId,
            @CorrelationId,
            @Source,
            @Status,
            @AttemptNumber,
            @Data,
            @ErrorCode,
            @ErrorMessage,
            @CreatedAt,
            @CreatedBy,
            @UpdatedAt,
            @UpdatedBy,
            @IsDeleted
        );
    ";

    public const string GetEventByEventId = @"
        SELECT *
        FROM [Event]
        WHERE EventId = @EventId
          AND IsDeleted = 0;
    ";

    public const string GetEventByJobId = """
    SELECT
        Id,
        EventId,
        EventType,
        EntityId,
        EntityType,
        UserId,
        JobId,
        MessageId,
        CorrelationId,
        Source,
        Status,
        AttemptNumber,
        Data,
        ErrorCode,
        ErrorMessage,
        CreatedAt,
        CreatedBy,
        UpdatedAt,
        UpdatedBy,
        IsDeleted
    FROM [dbo].[Event]
    WHERE JobId = @JobId
      AND IsDeleted = 0;
    """;

    public const string GetEventsByEntityId = @"
        SELECT *
        FROM [Event]
        WHERE EntityId = @EntityId
          AND IsDeleted = 0
        ORDER BY CreatedAt ASC;
    ";

    public const string GetEventsByUserId = @"
        SELECT *
        FROM [Event]
        WHERE UserId = @UserId
          AND IsDeleted = 0
        ORDER BY CreatedAt DESC;
    ";

    public const string UpdateEvent = @"
        UPDATE [Event]
        SET
            Status = @Status,
            AttemptNumber = @AttemptNumber,
            Data = @Data,
            ErrorCode = @ErrorCode,
            ErrorMessage = @ErrorMessage,
            UpdatedAt = @UpdatedAt,
            UpdatedBy = @UpdatedBy
        WHERE EventId = @EventId
          AND IsDeleted = 0;
    ";

    public const string DeleteEvent = @"
        UPDATE [Event]
        SET
            IsDeleted = 1,
            UpdatedAt = @UpdatedAt,
            UpdatedBy = @UpdatedBy
        WHERE EventId = @EventId
          AND IsDeleted = 0;
    ";
}
