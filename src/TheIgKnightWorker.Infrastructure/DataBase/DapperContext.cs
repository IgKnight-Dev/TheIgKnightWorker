using Microsoft.Azure.Amqp.Sasl;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Npgsql;
using Polly;
using System.Data;

namespace TheIgKnightWorker.Infrastructure.DataBase;

public class DapperContext : IDisposable
{
    private readonly string _connectionString;

    private IDbConnection? _connection;
    private IDbTransaction? _transaction;

    private bool _disposed;

    public DapperContext(IConfiguration configuration)
    {
        _connectionString =
            configuration["DbConnectionStringPostgre"]
            ?? configuration.GetConnectionString("DbConnectionString")
            ?? throw new ArgumentNullException(
                "DbConnectionString",
                "Connection string 'DbConnectionString' is missing from configuration.");
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
            throw new InvalidOperationException(
                "Transaction already started.");

        _transaction = _connection!.BeginTransaction();
    }

    public void CommitTransaction()
    {
        if (_transaction == null)
            throw new InvalidOperationException(
                "No active transaction to commit.");

        _transaction.Commit();
        _transaction.Dispose();
        _transaction = null;
    }

    public void RollbackTransaction()
    {
        if (_transaction == null)
            return;

        try
        {
            _transaction.Rollback();
        }
        finally
        {
            _transaction.Dispose();
            _transaction = null;
        }
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        try
        {
            _transaction?.Dispose();
            _connection?.Dispose();
        }
        finally
        {
            _transaction = null;
            _connection = null;
            _disposed = true;
        }
    }

    private void EnsureConnectionOpen()
    {
        if (_connection is { State: ConnectionState.Open })
            return;

        var builder =
            new NpgsqlConnectionStringBuilder(_connectionString)
            {
                Timeout = 60,
                CommandTimeout = 60,

                Pooling = true,

                // Important for Neon:
                MinPoolSize = 0,
                MaxPoolSize = 20,

                SslMode = SslMode.Require
            };

        var retryPolicy = Policy
            .Handle<NpgsqlException>()
            .Or<TimeoutException>()
            .WaitAndRetry(
                retryCount: 5,
                sleepDurationProvider: retryAttempt =>
                    TimeSpan.FromSeconds(
                        Math.Pow(2, retryAttempt)),
                onRetry: (exception, delay, retryCount, context) =>
                {
                    Console.WriteLine(
                        $"DB Retry {retryCount} after " +
                        $"{delay.TotalSeconds:F0}s: " +
                        $"{exception.Message}");
                });

        retryPolicy.Execute(() =>
        {
            _connection?.Dispose();

            _connection =
                new NpgsqlConnection(
                    builder.ConnectionString);

            _connection.Open();
        });
    }
}