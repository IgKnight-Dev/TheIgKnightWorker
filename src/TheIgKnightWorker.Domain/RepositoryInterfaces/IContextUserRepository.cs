using TheIgKnightWorker.Domain.Entities;

namespace TheIgKnightWorker.Domain.RepositoryInterfaces;

public interface IContextUserRepository
{
    Task<User> GetUserByUserIdAsync(Guid userId);
}
