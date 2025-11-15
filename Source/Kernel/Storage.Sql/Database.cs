// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Data.Common;
using System.Diagnostics.CodeAnalysis;
using Cratis.Applications.EntityFrameworkCore;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Configuration;
using Cratis.Chronicle.Storage.Sql.Cluster;
using Cratis.Chronicle.Storage.Sql.EventStores;
using Cratis.Chronicle.Storage.Sql.EventStores.Namespaces;
using Cratis.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Cratis.Chronicle.Storage.Sql;

/// <summary>
/// Represents an implementation of <see cref="IDatabase"/>.
/// </summary>
/// <param name="serviceProvider">The <see cref="IServiceProvider"/>.</param>
/// <param name="options">The <see cref="IOptions{ChronicleOptions}"/>.</param>
[Singleton]
public class Database(IServiceProvider serviceProvider, IOptions<ChronicleOptions> options) : IDatabase
{
    readonly AsyncLocal<ClusterDbContext> _clusterDbContext = new();
    readonly AsyncLocal<Dictionary<string, EventStoreDbContext>> _eventStoreDbContexts = new();
    readonly AsyncLocal<Dictionary<string, Dictionary<string, NamespaceDbContext>>> _namespaceDbContexts = new();
    readonly AsyncLocal<Dictionary<string, Dictionary<string, Dictionary<string, UniqueConstraintDbContext>>>> _uniqueConstraintDbContexts = new();

    /// <inheritdoc/>
    public Task<DbContextScope<ClusterDbContext>> Cluster()
    {
        if (_clusterDbContext.Value == null)
        {
            var builder = new DbContextOptionsBuilder<ClusterDbContext>();
            builder.UseDatabaseFromConnectionString(options.Value.Storage.ConnectionDetails);
            builder.UseApplicationServiceProvider(serviceProvider);
            _clusterDbContext.Value = new ClusterDbContext(builder.Options);
        }

        return Task.FromResult(new DbContextScope<ClusterDbContext>(_clusterDbContext.Value, () => _clusterDbContext.Value = null!));
    }

    /// <inheritdoc/>
    public async Task<DbContextScope<EventStoreDbContext>> EventStore(EventStoreName eventStore)
    {
        _eventStoreDbContexts.Value ??= new Dictionary<string, EventStoreDbContext>();

        if (!_eventStoreDbContexts.Value.TryGetValue(eventStore.Value, out var dbContext))
        {
            var builder = new DbContextOptionsBuilder<EventStoreDbContext>();
            var connectionString = GetConnectionStringForEventStore(eventStore);
            builder.UseDatabaseFromConnectionString(connectionString);
            builder.UseApplicationServiceProvider(serviceProvider);

            dbContext = new EventStoreDbContext(builder.Options);
            await dbContext.Database.MigrateAsync();
            _eventStoreDbContexts.Value[eventStore.Value] = dbContext;
        }

        return new DbContextScope<EventStoreDbContext>(dbContext, () => _eventStoreDbContexts.Value?.Remove(eventStore.Value));
    }

    /// <inheritdoc/>
    public async Task<DbContextScope<NamespaceDbContext>> Namespace(EventStoreName eventStore, EventStoreNamespaceName @namespace)
    {
        var key = $"{eventStore.Value}:{@namespace.Value}";

        _namespaceDbContexts.Value ??= new Dictionary<string, Dictionary<string, NamespaceDbContext>>();
        if (!_namespaceDbContexts.Value.TryGetValue(eventStore.Value, out var namespaces))
        {
            namespaces = new Dictionary<string, NamespaceDbContext>();
            _namespaceDbContexts.Value[eventStore.Value] = namespaces;
        }

        if (!namespaces.TryGetValue(key, out var dbContext))
        {
            var builder = new DbContextOptionsBuilder<NamespaceDbContext>();
            var connectionString = GetConnectionStringForEventStoreAndNamespace(eventStore, @namespace);
            builder.UseDatabaseFromConnectionString(connectionString);
            builder.UseApplicationServiceProvider(serviceProvider);

            dbContext = new NamespaceDbContext(builder.Options);
            await dbContext.Database.MigrateAsync();
            namespaces[@namespace.Value] = dbContext;
        }

        return new DbContextScope<NamespaceDbContext>(dbContext, () => namespaces.Remove(key));
    }

    /// <inheritdoc/>
    public async Task<DbContextScope<UniqueConstraintDbContext>> UniqueConstraintTable(EventStoreName eventStore, EventStoreNamespaceName @namespace, string constraintName)
    {
        var key = $"{eventStore.Value}:{@namespace.Value}:{constraintName}";

        _uniqueConstraintDbContexts.Value ??= new Dictionary<string, Dictionary<string, Dictionary<string, UniqueConstraintDbContext>>>();
        if (!_uniqueConstraintDbContexts.Value.TryGetValue(eventStore.Value, out var namespaces))
        {
            namespaces = new Dictionary<string, Dictionary<string, UniqueConstraintDbContext>>();
            _uniqueConstraintDbContexts.Value[eventStore.Value] = namespaces;
        }

        if (!namespaces.TryGetValue(@namespace.Value, out var constraints))
        {
            constraints = new Dictionary<string, UniqueConstraintDbContext>();
            namespaces[@namespace.Value] = constraints;
        }

        if (!constraints.TryGetValue(constraintName, out var dbContext))
        {
            var builder = new DbContextOptionsBuilder<UniqueConstraintDbContext>();
            var connectionString = GetConnectionStringForEventStoreAndNamespace(eventStore, @namespace);
            builder.UseDatabaseFromConnectionString(connectionString);
            builder.UseApplicationServiceProvider(serviceProvider);

            dbContext = new UniqueConstraintDbContext(builder.Options, constraintName);
            await dbContext.EnsureTableExists();
            constraints[constraintName] = dbContext;
        }

        return new DbContextScope<UniqueConstraintDbContext>(dbContext, () => constraints.Remove(constraintName));
    }

    string GetConnectionStringForEventStore(EventStoreName eventStore)
    {
        var databaseType = options.Value.Storage.ConnectionDetails.GetDatabaseType();
        if (databaseType == DatabaseType.Sqlite)
        {
            return ReplaceFilename(eventStore);
        }

        return options.Value.Storage.ConnectionDetails;
    }

    string GetConnectionStringForEventStoreAndNamespace(EventStoreName eventStore, EventStoreNamespaceName @namespace)
    {
        var databaseType = options.Value.Storage.ConnectionDetails.GetDatabaseType();
        if (databaseType == DatabaseType.Sqlite)
        {
            return ReplaceFilename($"{eventStore}_{@namespace}");
        }

        return options.Value.Storage.ConnectionDetails;
    }

    string ReplaceFilename(string postfix)
    {
        if (TryReplaceFilename("Data Source", postfix, out var dataSource))
        {
            return dataSource;
        }
        if (TryReplaceFilename("Filename", postfix, out var filename))
        {
            return filename;
        }

        return options.Value.Storage.ConnectionDetails;
    }

    bool TryReplaceFilename(string keyToReplace, string postfix, [NotNullWhen(true)] out string? connectionString)
    {
        var builder = new DbConnectionStringBuilder
        {
            ConnectionString = options.Value.Storage.ConnectionDetails
        };

        if (builder.TryGetValue(keyToReplace, out var dataSource))
        {
            var originalFilename = dataSource.ToString();
            var directory = Path.GetDirectoryName(originalFilename) ?? string.Empty;
            var newFilename = $"{Path.GetFileNameWithoutExtension(originalFilename)}_{postfix}{Path.GetExtension(originalFilename)}";
            builder[keyToReplace] = Path.Combine(directory, newFilename);
            connectionString = builder.ConnectionString;
            return true;
        }

        connectionString = null;
        return false;
    }
}
