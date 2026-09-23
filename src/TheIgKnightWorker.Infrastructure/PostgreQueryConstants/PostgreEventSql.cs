namespace TheIgKnightWorker.Infrastructure.PostgreQueryConstants;

internal class PostgreEventSql
{
    public const string CreateEvent = @"
        INSERT INTO Event
        (EventId, EventType, EntityId, EntityType, UserId, JobId, MessageId, CorrelationId, Source,
         Status, AttemptNumber, Data, ErrorCode, ErrorMessage, CreatedAt, CreatedBy, UpdatedAt, UpdatedBy, IsDeleted)
        VALUES
        (@EventId, @EventType, @EntityId, @EntityType, @UserId, @JobId, @MessageId, @CorrelationId, @Source,
         @Status, @AttemptNumber, @Data, @ErrorCode, @ErrorMessage, @CreatedAt, @CreatedBy, @UpdatedAt, @UpdatedBy, @IsDeleted);";

    public const string GetEventByEventId = @"
        SELECT *
        FROM Event
        WHERE EventId = @EventId
          AND IsDeleted = FALSE;";

    public const string GetEventsByJobId = @"
        SELECT *
        FROM Event
        WHERE JobId = @JobId
          AND IsDeleted = FALSE
        ORDER BY CreatedAt ASC;";

    public const string GetEventsByEntityId = @"
        SELECT *
        FROM Event
        WHERE EntityId = @EntityId
          AND IsDeleted = FALSE
        ORDER BY CreatedAt ASC;";

    public const string GetEventsByUserId = @"
        SELECT *
        FROM Event
        WHERE UserId = @UserId
          AND IsDeleted = FALSE
        ORDER BY CreatedAt DESC;";

    public const string UpdateEvent = @"
        UPDATE Event
        SET Status = @Status,
            AttemptNumber = @AttemptNumber,
            Data = @Data,
            ErrorCode = @ErrorCode,
            ErrorMessage = @ErrorMessage,
            UpdatedAt = @UpdatedAt,
            UpdatedBy = @UpdatedBy
        WHERE EventId = @EventId
          AND IsDeleted = FALSE;";

    public const string DeleteEvent = @"
        UPDATE Event
        SET IsDeleted = TRUE,
            UpdatedAt = @UpdatedAt,
            UpdatedBy = @UpdatedBy
        WHERE EventId = @EventId
          AND IsDeleted = FALSE;";

    public const string GetEventByJobId = @"
        SELECT Id, EventId, EventType, EntityId, EntityType, UserId, JobId, MessageId,
               CorrelationId, Source, Status, AttemptNumber, Data, ErrorCode, ErrorMessage,
               CreatedAt, CreatedBy, UpdatedAt, UpdatedBy, IsDeleted
        FROM Event
        WHERE JobId = @JobId
          AND IsDeleted = FALSE;";
}
