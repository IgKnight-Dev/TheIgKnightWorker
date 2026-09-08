using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Polly;
using System.Data;

namespace TheIgKnightWorker.Infrastructure.DataBase;

public class DapperContext : IDisposable
{
    private readonly string _connectionString;
    private IDbConnection? _connection;
    private IDbTransaction? _transaction;
    private bool disposed;

    public DapperContext(IConfiguration configuration)
    {
        _connectionString = configuration["DbConnectionString"];

        if (string.IsNullOrWhiteSpace(_connectionString))
        {
            // Try fetching from configuration.GetConnectionString() as a fallback
            _connectionString = configuration.GetConnectionString("DbConnectionString");

            if (string.IsNullOrWhiteSpace(_connectionString))
            {
                throw new ArgumentNullException("DbConnectionString", "Connection string 'DbConnectionString' is missing from both configuration and connection strings.");
            }
        }
    }

    public IDbConnection Connection
    {
        get
        {
            EnsureConnectionOpen();
            return _connection!;
        }
    }

    public IDbTransaction? CurrentTransaction => _transaction;

    public void BeginTransaction()
    {
        EnsureConnectionOpen();

        if (_transaction != null)
            throw new InvalidOperationException("Transaction already started.");

        _transaction = _connection!.BeginTransaction();
    }

    public void CommitTransaction()
    {
        if (_transaction == null)
            throw new InvalidOperationException("No active transaction to commit.");

        _transaction.Commit();
        _transaction.Dispose();
        _transaction = null;
    }

    public void RollbackTransaction()
    {
        if (_transaction == null)
            return;

        _transaction.Rollback();
        _transaction.Dispose();
        _transaction = null;
    }

    public void Dispose()
    {
        if (disposed)
            return;

        if (_connection is { State: not ConnectionState.Closed })
        {
            _connection.Close();
        }

        _connection?.Dispose();
        disposed = true;
    }

    private void EnsureConnectionOpen()
    {
        if (_connection is { State: ConnectionState.Open })
            return;

        var builder = new SqlConnectionStringBuilder(_connectionString)
        {
            ConnectTimeout = 60,

            // Built-in SqlClient retry
            ConnectRetryCount = 5,
            ConnectRetryInterval = 10,

            Pooling = true,
            TrustServerCertificate = true
        };

        var retryPolicy = Policy
            .Handle<SqlException>()
            .Or<TimeoutException>()
            .WaitAndRetry(
                retryCount: 3,
                sleepDurationProvider: retryAttempt =>
                    TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),
                onRetry: (exception, delay, retryCount, context) =>
                {
                    Console.WriteLine(
                        $"DB Retry {retryCount} after {delay.TotalSeconds}s: {exception.Message}");
                });

        retryPolicy.Execute(() =>
        {
            _connection = new SqlConnection(builder.ConnectionString);

            _connection.Open();
        });
    }

}
