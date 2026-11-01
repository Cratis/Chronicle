// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Threading.Channels;
using Cratis.Chronicle.Diagnostics;
using Cratis.Chronicle.Storage.Sql.Diagnostics;
using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace Cratis.Chronicle.Storage.Sql.Sinks;

/// <summary>
/// Provides isolated SQL Server databases for replay promotion specs.
/// </summary>
public sealed class SqlServerFixture : IAsyncLifetime
{
    const int Port = 1433;
    const int DatabaseInUse = 3702;
    static readonly TimeSpan _prefetchTimeout = TimeSpan.FromSeconds(60);
    const string Password = "Chronicle4476!Test";
    readonly Channel<(string? Name, TaskCompletionSource? Flush)> _drops = Channel.CreateUnbounded<(string? Name, TaskCompletionSource? Flush)>(new() { SingleReader = true });
    readonly Channel<string> _ready = Channel.CreateBounded<string>(2);
    readonly CancellationTokenSource _stop = new();
#pragma warning disable CA2213 // DisposeAsync disposes the container through Timings.Run, which the analyzer cannot follow.
    IContainer _container;
#pragma warning restore CA2213
    SqlCommandTimings? _commandTimings;
    Task _dropWorker = Task.CompletedTask;
    Task _producer = Task.CompletedTask;
    int _dropFallbacks;
    int _dropFailures;

    internal IntegrationTimings Timings { get; } = new(nameof(SqlServerFixture), "sql-specs", "mssql");

    internal T Track<T>(T context)
        where T : DbContext
    {
        if (!IntegrationTimings.Enabled) return context;
        if (_commandTimings is null)
        {
            _commandTimings = new(Timings);
            _commandTimings.Start();
        }
        return _commandTimings.Track(context);
    }

    string ConnectionString => $"Server=127.0.0.1,{_container.GetMappedPublicPort(Port)};Database=master;User Id=sa;Password={Password};TrustServerCertificate=true";

    /// <summary>
    /// Creates a database owned by one specification.
    /// </summary>
    /// <returns>The database connection string.</returns>
    public async Task<string> CreateDatabase()
    {
        var name = string.Empty;
        await Timings.Run("sql-database-wait", async () => name = await TakePrefetched());
        return new SqlConnectionStringBuilder(ConnectionString) { InitialCatalog = name }.ConnectionString;
    }

    /// <summary>
    /// Queues the drop of a specification's database and returns without waiting for it; a background worker drops it.
    /// </summary>
    /// <param name="connectionString">The connection string returned by <see cref="CreateDatabase"/>.</param>
    /// <returns>Awaitable task.</returns>
    public Task DropDatabase(string connectionString)
    {
        var name = new SqlConnectionStringBuilder(connectionString).InitialCatalog;
        _drops.Writer.TryWrite((name, null));
        return Task.CompletedTask;
    }

    /// <summary>
    /// Completes when every drop queued before the call has finished.
    /// </summary>
    /// <returns>Awaitable task.</returns>
    internal Task FlushDrops()
    {
        var flush = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        if (!_drops.Writer.TryWrite((null, flush)))
        {
            flush.TrySetResult();
        }
        return flush.Task;
    }

    /// <inheritdoc/>
    public async ValueTask InitializeAsync()
    {
        _container = new ContainerBuilder("mcr.microsoft.com/mssql/server:2022-latest")
            .WithEnvironment("ACCEPT_EULA", "Y")
            .WithEnvironment("MSSQL_SA_PASSWORD", Password)
            .WithPortBinding(Port, assignRandomHostPort: true)
            .WithWaitStrategy(Wait.ForUnixContainer().UntilCommandIsCompleted(
                ["/opt/mssql-tools18/bin/sqlcmd", "-C", "-S", "tcp:127.0.0.1,1433", "-U", "sa", "-P", Password, "-l", "3", "-Q", "SELECT 1"],
                wait => wait.WithTimeout(TimeSpan.FromSeconds(90))))
            .Build();
        await Timings.Run("sql-container-start", () => _container.StartAsync());

        // New databases inherit model's options. Only crash durability changes, and the spec container is never crashed.
        await Timings.Run("sql-model-durability", () => Execute("ALTER DATABASE model SET DELAYED_DURABILITY = FORCED"));
        _dropWorker = StartDetached(RunDrops);
        _producer = StartDetached(ProduceDatabases);
    }

    /// <inheritdoc/>
    public async ValueTask DisposeAsync()
    {
        try
        {
            _drops.Writer.TryComplete();
            _ready.Writer.TryComplete();
            await _stop.CancelAsync();
            try
            {
                await Task.WhenAll(_dropWorker, _producer);
            }
            catch (OperationCanceledException)
            {
                // Cancelled on purpose; the container is discarded with whatever was still queued.
            }

            Console.Error.WriteLine($"SqlServerFixture background drops: fallbacks={_dropFallbacks}, failures={_dropFailures}");
            if (_container is not null)
            {
                await Timings.Run("sql-container-disposal", () => _container.DisposeAsync().AsTask());
            }
        }
        finally
        {
            _commandTimings?.Dispose();
            _stop.Dispose();
            Timings.Complete();
        }
    }

    // Runs detached from the caller's execution context so the worker's timing phases are not parented to whichever spec phase is current.
    static Task StartDetached(Func<Task> work)
    {
        using (ExecutionContext.SuppressFlow())
        {
            return Task.Run(work);
        }
    }

    async Task<string> TakePrefetched()
    {
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(_stop.Token);
        timeout.CancelAfter(_prefetchTimeout);
        try
        {
            return await _ready.Reader.ReadAsync(timeout.Token);
        }
        catch (OperationCanceledException) when (!_stop.IsCancellationRequested)
        {
            throw new TimeoutException($"No prefetched SQL Server database became ready within {_prefetchTimeout.TotalSeconds} seconds; the producer task is {_producer.Status}.");
        }
    }

    async Task ProduceDatabases()
    {
        try
        {
            while (!_stop.IsCancellationRequested)
            {
                var name = $"spec_{Guid.NewGuid():N}";
                await Timings.Run("sql-database-create", () => Execute($"CREATE DATABASE [{name}]", cancellationToken: _stop.Token));
                await _ready.Writer.WriteAsync(name, _stop.Token);
            }
        }
        catch (Exception) when (_stop.IsCancellationRequested)
        {
            // Disposing; prefetched databases are discarded together with the container.
        }
        catch (Exception ex)
        {
            // Faults the next CreateDatabase so specs fail loudly instead of waiting for a database that never comes.
            _ready.Writer.TryComplete(ex);
        }
    }

    async Task RunDrops()
    {
        try
        {
            await foreach (var (name, flush) in _drops.Reader.ReadAllAsync(_stop.Token))
            {
                if (flush is not null)
                {
                    flush.TrySetResult();
                    continue;
                }

                try
                {
                    await Timings.Run("sql-database-drop-background", () => Drop(name!));
                }
                catch (Exception ex) when (!_stop.IsCancellationRequested)
                {
                    Interlocked.Increment(ref _dropFailures);
                    Console.Error.WriteLine($"SqlServerFixture could not drop '{name}': {ex.Message}");
                }
            }
        }
        catch (Exception) when (_stop.IsCancellationRequested)
        {
            // Disposing; remaining drops are discarded together with the container.
        }
    }

    async Task Drop(string name)
    {
        var escaped = name.Replace("]", "]]", StringComparison.Ordinal);

        // Evicts only this database's sessions on the server, so unrelated pools stay warm. The name is unique per specification.
        var sql = $"DECLARE @k nvarchar(max) = N''; SELECT @k += N'KILL ' + CAST(session_id AS nvarchar(10)) + N';' FROM (SELECT session_id FROM sys.dm_exec_sessions WHERE database_id = DB_ID(@name) UNION SELECT request_session_id FROM sys.dm_tran_locks WHERE resource_type = 'DATABASE' AND resource_database_id = DB_ID(@name)) s WHERE session_id <> @@SPID; EXEC (@k); DROP DATABASE [{escaped}]";
        if (await TryDrop(sql, name) || await TryDrop(sql, name))
        {
            return;
        }

        // A connection outside the pool is still open; SINGLE_USER ... ROLLBACK IMMEDIATE evicts it, at a cost of seconds.
        Interlocked.Increment(ref _dropFallbacks);
        await Execute($"ALTER DATABASE [{escaped}] SET SINGLE_USER WITH ROLLBACK IMMEDIATE; DROP DATABASE [{escaped}]", cancellationToken: _stop.Token);
    }

    async Task<bool> TryDrop(string sql, string name)
    {
        try
        {
            await Execute(sql, name, _stop.Token);
            return true;
        }
        catch (SqlException ex) when (ex.Number == DatabaseInUse)
        {
            // Killed sessions may still be rolling back.
            await Task.Delay(100, _stop.Token);
            return false;
        }
    }

    async Task Execute(string sql, string? name = null, CancellationToken cancellationToken = default)
    {
        await using var connection = new SqlConnection(ConnectionString);
        await connection.OpenAsync(cancellationToken);
        await using var command = connection.CreateCommand();
#pragma warning disable CA2100 // Only fixture-owned database identifiers, delimited and escaped by the callers.
        command.CommandText = sql;
#pragma warning restore CA2100
        if (name is not null)
        {
            command.Parameters.Add(new SqlParameter("@name", name));
        }
        await command.ExecuteNonQueryAsync(cancellationToken);
    }
}
