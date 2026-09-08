using TheIgKnightWorker.Domain.RepositoryInterfaces;

namespace TheIgKnightWorker.Domain.UnitOfWork;

public interface IUnitOfWork : IDisposable
{
    IDocumentRepository DocumentRepository { get; }

    ISkillRepository SkillRepository { get; }
    IContextUserRepository ContextUserRepository { get; }

    void BeginTransaction();
    void Commit();
    void Rollback();
}
