using TheIgKnightWorker.Infrastructure.DbModels;

namespace TheIgKnightWorker.Infrastructure.Repositories;

public class EventRepository : IEventRepository
{
    private readonly DapperContext DapperContext;
    private readonly IMapper Mapper;

    public EventRepository(
        DapperContext dapperContext,
        IMapper mapper)
    {
        this.DapperContext = dapperContext;
        this.Mapper = mapper;
    }

    public async Task<long> CreateEventAsync(Event eventEntity)
    {
        var connection = this.DapperContext.Connection;

        var dbEvent =
            this.Mapper.Map<DbEvent>(eventEntity);

        await connection.ExecuteAsync(
            PostgreEventSql.CreateEvent,
            dbEvent,
            this.DapperContext.CurrentTransaction);

        return dbEvent.Id;
    }

    public async Task<Event?> GetEventByEventIdAsync(
        Guid eventId)
    {
        var connection =
            this.DapperContext.Connection;

        var dbEvent =
            await connection.QuerySingleOrDefaultAsync<DbEvent>(
                PostgreEventSql.GetEventByEventId,
                new
                {
                    EventId = eventId
                },
                this.DapperContext.CurrentTransaction);

        return this.Mapper.Map<Event?>(dbEvent);
    }

    public async Task<Event?> GetEventByJobIdAsync(
        Guid jobId)
    {
        var connection =
            this.DapperContext.Connection;

        var dbEvent =
            await connection.QuerySingleOrDefaultAsync<DbEvent>(
                PostgreEventSql.GetEventByJobId,
                new
                {
                    JobId = jobId
                },
                this.DapperContext.CurrentTransaction);

        return this.Mapper.Map<Event?>(dbEvent);
    }

    public async Task<IEnumerable<Event>> GetEventsByEntityIdAsync(
        Guid entityId)
    {
        var connection =
            this.DapperContext.Connection;

        var dbEvents =
            await connection.QueryAsync<DbEvent>(
                PostgreEventSql.GetEventsByEntityId,
                new
                {
                    EntityId = entityId
                },
                this.DapperContext.CurrentTransaction);

        return this.Mapper.Map<IEnumerable<Event>>(dbEvents);
    }

    public async Task<IEnumerable<Event>> GetEventsByUserIdAsync(
        Guid userId)
    {
        var connection =
            this.DapperContext.Connection;

        var dbEvents =
            await connection.QueryAsync<DbEvent>(
                PostgreEventSql.GetEventsByUserId,
                new
                {
                    UserId = userId
                },
                this.DapperContext.CurrentTransaction);

        return this.Mapper.Map<IEnumerable<Event>>(dbEvents);
    }

    public async Task<bool> UpdateEventAsync(
        Event eventEntity)
    {
        var connection =
            this.DapperContext.Connection;

        var dbEvent =
            this.Mapper.Map<DbEvent>(eventEntity);

        var affectedRows =
            await connection.ExecuteAsync(
                PostgreEventSql.UpdateEvent,
                dbEvent,
                this.DapperContext.CurrentTransaction);

        return affectedRows > 0;
    }

    public async Task<bool> DeleteEventAsync(
        Guid eventId,
        Guid? updatedBy = null)
    {
        var connection =
            this.DapperContext.Connection;

        var affectedRows =
            await connection.ExecuteAsync(
                PostgreEventSql.DeleteEvent,
                new
                {
                    EventId = eventId,
                    UpdatedAt = DateTime.UtcNow,
                    UpdatedBy = updatedBy
                },
                this.DapperContext.CurrentTransaction);

        return affectedRows > 0;
    }
}