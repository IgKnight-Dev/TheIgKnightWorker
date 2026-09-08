using Autofac;
using System.Data;
using TheIgKnightWorker.Domain.UnitOfWork;

namespace TheIgKnightWorker.Infrastructure.Repositories;

internal class UnitOfWork : IUnitOfWork
{
    private readonly ILifetimeScope _scope;

    private readonly DapperContext _dapperContext;


    private IDocumentRepository? _documentRepository;
    private ISkillRepository _skillRepository;
    private IContextUserRepository _contextUserRepository;


    public UnitOfWork(DapperContext dapperContext, ILifetimeScope scope)
    {
        this._dapperContext = dapperContext;
        this._scope = scope;
    }

    public IDocumentRepository DocumentRepository =>
        this._documentRepository ??= _scope.Resolve<IDocumentRepository>(
            new TypedParameter(typeof(IDbConnection), _dapperContext.Connection),
            new NamedParameter("transaction", _dapperContext.CurrentTransaction)
        );

    public ISkillRepository SkillRepository =>
       this._skillRepository ??= _scope.Resolve<ISkillRepository>(
           new TypedParameter(typeof(IDbConnection), _dapperContext.Connection),
           new NamedParameter("transaction", _dapperContext.CurrentTransaction)
       );

    public IContextUserRepository ContextUserRepository =>
       this._contextUserRepository ??= _scope.Resolve<IContextUserRepository>(
           new TypedParameter(typeof(IDbConnection), _dapperContext.Connection),
           new NamedParameter("transaction", _dapperContext.CurrentTransaction)
       );

    public void BeginTransaction() => _dapperContext.BeginTransaction();

    public void Commit()
    {
        _dapperContext.CommitTransaction();
        Dispose();
    }

    public void Rollback()
    {
        _dapperContext.RollbackTransaction();
        Dispose();
    }

    public void Dispose() => _dapperContext.Dispose();
}

