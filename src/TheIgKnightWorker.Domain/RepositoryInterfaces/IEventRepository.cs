namespace TheIgKnightWorker.Domain.RepositoryInterfaces;

public interface IEventRepository
{
    Task<long> CreateEventAsync(Event eventEntity);

    Task<Event?> GetEventByEventIdAsync(Guid eventId);

    Task<Event?> GetEventByJobIdAsync(
        Guid jobId);

    Task<IEnumerable<Event>> GetEventsByEntityIdAsync(Guid entityId);

    Task<IEnumerable<Event>> GetEventsByUserIdAsync(Guid userId);

    Task<bool> UpdateEventAsync(Event eventEntity);

    Task<bool> DeleteEventAsync(
        Guid eventId,
        Guid? updatedBy = null);
}