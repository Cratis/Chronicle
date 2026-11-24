// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Connections;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Storage.Sql;
using Cratis.Chronicle.Storage.Sql.Cluster;
using Cratis.Chronicle.Storage.Sql.EventStores;
using Cratis.Chronicle.Storage.Sql.EventStores.Namespaces;
using Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.EventSequences;
using Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.ReadModels;
using Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.UniqueConstraints;
using Cratis.Chronicle.Storage.Sql.Sinks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;

namespace Cratis.Chronicle.Benchmarks.Storage.Hosts;

/// <summary>Non-recording benchmark adapter; scopes own fresh contexts over cached options.</summary>
/// <param name="options">The immutable context options for the host.</param>
internal sealed class BenchmarkDatabase(SqlBenchmarkOptions options) : IDatabase, IDisposable
{
    static readonly Action _onDispose = () => { };
    readonly Lazy<ServiceProvider> _readModelServices = new(options.CreateReadModelServices);
    readonly HashSet<string> _ensuredTables = [];
    readonly EventSequenceMigrator _events = new(
        new TableMigrator<EventSequenceDbContext>(NullLogger<TableMigrator<EventSequenceDbContext>>.Instance),
        NullLogger<EventSequenceMigrator>.Instance);
    readonly UniqueConstraintMigrator _constraints = new(
        new TableMigrator<UniqueConstraintDbContext>(NullLogger<TableMigrator<UniqueConstraintDbContext>>.Instance),
        new TableMigrator<UniqueConstraintValuesDbContext>(NullLogger<TableMigrator<UniqueConstraintValuesDbContext>>.Instance),
        NullLogger<UniqueConstraintMigrator>.Instance);

    /// <inheritdoc/>
    public TimeSpan LiveQueryPollingInterval => TimeSpan.FromSeconds(1);

    /// <inheritdoc/>
    public Task<DbContextScope<EventSequenceDbContext>> EventSequenceTable(EventStoreName eventStore, EventStoreNamespaceName @namespace, string eventSequenceName) =>
        Task.FromResult(CreateEventSequenceScope(eventSequenceName));

    /// <inheritdoc/>
    public Task<DbContextScope<NamespaceDbContext>> Namespace(EventStoreName eventStore, EventStoreNamespaceName @namespace) =>
        Task.FromResult(CreateNamespaceScope());

    /// <inheritdoc/>
    public Task<DbContextScope<NamespaceDbContext>?> ExistingNamespace(EventStoreName eventStore, EventStoreNamespaceName @namespace) =>
        Task.FromResult<DbContextScope<NamespaceDbContext>?>(CreateNamespaceScope());

    /// <inheritdoc/>
    public async Task<DbContextScope<UniqueConstraintDbContext>> UniqueConstraintTable(EventStoreName eventStore, EventStoreNamespaceName @namespace, string constraintName)
    {
#pragma warning disable CA2000 // Ownership transfers to the returned scope.
        var context = new UniqueConstraintDbContext(options.Constraints, constraintName, _constraints);
#pragma warning restore CA2000
        try
        {
            bool ensure;
            lock (_ensuredTables) ensure = _ensuredTables.Add(constraintName);
            if (ensure) await context.EnsureTableExists();
            return new(context, _onDispose);
        }
        catch
        {
            await context.DisposeAsync();
            throw;
        }
    }

    /// <inheritdoc/>
    public Task<DbContextScope<UniqueConstraintValuesDbContext>> UniqueConstraintValuesTable(EventStoreName eventStore, EventStoreNamespaceName @namespace, string constraintName) => throw new NotSupportedException();

    /// <inheritdoc/>
    public void ClearTableMigrationCache(string connectionStringPrefix)
    {
    }

    /// <inheritdoc/>
    public Task<DbContextScope<ClusterDbContext>> Cluster() => throw new NotSupportedException();

    /// <inheritdoc/>
    public Task<DbContextScope<EventStoreDbContext>> EventStore(EventStoreName eventStore) => throw new NotSupportedException();

    /// <inheritdoc/>
    public DbContextOptions<Cratis.Choreography.Storage.Sql.Jobs.JobsDbContext> GetJobsDbContextOptions(string eventStore, string @namespace) => throw new NotSupportedException();

    /// <inheritdoc/>
    public Task<DbContextScope<ReadModelDbContext>> ReadModelTable(EventStoreName eventStore, EventStoreNamespaceName @namespace, string containerName, IReadOnlyList<ProjectedColumn> columns, ConnectionPurpose purpose = ConnectionPurpose.Runtime, SinkProvenanceTarget? admittedTarget = null) =>
        _readModelServices.Value.GetRequiredService<IDatabase>().ReadModelTable(eventStore, @namespace, containerName, columns, purpose, admittedTarget);

    /// <inheritdoc/>
    public Task<DbContextScope<ReadModelDbContext>?> ExistingReadModelTable(EventStoreName eventStore, EventStoreNamespaceName @namespace, string containerName, IReadOnlyList<ProjectedColumn> columns, SinkProvenanceTarget? admittedTarget = null) =>
        _readModelServices.Value.GetRequiredService<IDatabase>().ExistingReadModelTable(eventStore, @namespace, containerName, columns, admittedTarget);

    /// <inheritdoc/>
    public Task<bool> NamespaceHasEvents(EventStoreName eventStore, EventStoreNamespaceName @namespace) => throw new NotSupportedException();
#if DEVELOPMENT
    /// <inheritdoc/>
    public Task Wipe() => throw new NotSupportedException();
#endif

    /// <inheritdoc/>
    public void Dispose()
    {
        if (_readModelServices.IsValueCreated)
        {
            _readModelServices.Value.Dispose();
        }
    }

    /// <summary>Creates an owned context scope without a Task wrapper for synchronous host setup.</summary>
    /// <param name="eventSequenceName">The event-sequence table name.</param>
    /// <returns>A fresh scope that owns and disposes its context.</returns>
    internal DbContextScope<EventSequenceDbContext> CreateEventSequenceScope(string eventSequenceName)
    {
#pragma warning disable CA2000 // Ownership of the context transfers to the returned DbContextScope.
        return new(new(options.Events, eventSequenceName, _events), _onDispose);
#pragma warning restore CA2000
    }

    DbContextScope<NamespaceDbContext> CreateNamespaceScope()
    {
#pragma warning disable CA2000 // Ownership of the context transfers to the returned DbContextScope.
        return new(new(options.Namespace), _onDispose);
#pragma warning restore CA2000
    }
}
