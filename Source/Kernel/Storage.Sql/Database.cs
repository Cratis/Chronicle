// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Data.Common;
using System.Diagnostics.CodeAnalysis;
using Cratis.Arc.EntityFrameworkCore;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Configuration;
using Cratis.Chronicle.Storage.Sql.Cluster;
using Cratis.Chronicle.Storage.Sql.EventStores;
using Cratis.Chronicle.Storage.Sql.EventStores.Namespaces;
using Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.EventSequences;
using Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.UniqueConstraints;
using Cratis.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Query;
using Microsoft.Extensions.Options;

namespace Cratis.Chronicle.Storage.Sql;

/// <summary>
/// Represents an implementation of <see cref="IDatabase"/>.
/// </summary>
/// <param name="serviceProvider">The <see cref="IServiceProvider"/>.</param>
/// <param name="options">The <see cref="IOptions{ChronicleOptions}"/>.</param>
/// <param name="eventSequenceMigrator">The <see cref="IEventSequenceMigrator"/> for managing event sequence table migrations.</param>
/// <param name="uniqueConstraintMigrator">The <see cref="IUniqueConstraintMigrator"/> for managing unique constraint table migrations.</param>
[Singleton]
public class Database(IServiceProvider serviceProvider, IOptions<ChronicleOptions> options, IEventSequenceMigrator eventSequenceMigrator, IUniqueConstraintMigrator uniqueConstraintMigrator) : IDatabase
{
    readonly AsyncLocal<ClusterDbContext> _clusterDbContext = new();
    readonly AsyncLocal<Dictionary<string, EventStoreDbContext>> _eventStoreDbContexts = new();
    readonly AsyncLocal<Dictionary<string, Dictionary<string, NamespaceDbContext>>> _namespaceDbContexts = new();
    readonly AsyncLocal<Dictionary<string, Dictionary<string, Dictionary<string, UniqueConstraintDbContext>>>> _uniqueConstraintDbContexts = new();
    readonly AsyncLocal<Dictionary<string, Dictionary<string, Dictionary<string, EventSequenceDbContext>>>> _eventSequenceDbContexts = new();

    /// <inheritdoc/>
    public Task<DbContextScope<ClusterDbContext>> Cluster()
    {
        if (_clusterDbContext.Value == null)
        {
            var builder = new DbContextOptionsBuilder<ClusterDbContext>();
            builder.UseDatabaseFromConnectionString(options.Value.Storage.ConnectionDetails);
            builder
                .UseApplicationServiceProvider(serviceProvider)
                .ReplaceService<IEvaluatableExpressionFilter, ConceptAsEvaluatableExpressionFilter>()
                .ReplaceService<IModelCustomizer, ConceptAsModelCustomizer>()
                .AddInterceptors(new ConceptAsQueryExpressionInterceptor(), new ConceptAsDbCommandInterceptor());
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
            builder
                .UseApplicationServiceProvider(serviceProvider)
                .ReplaceService<IEvaluatableExpressionFilter, ConceptAsEvaluatableExpressionFilter>()
                .ReplaceService<IModelCustomizer, ConceptAsModelCustomizer>()
                .AddInterceptors(new ConceptAsQueryExpressionInterceptor(), new ConceptAsDbCommandInterceptor());

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
            builder
                .UseApplicationServiceProvider(serviceProvider)
                .ReplaceService<IEvaluatableExpressionFilter, ConceptAsEvaluatableExpressionFilter>()
                .ReplaceService<IModelCustomizer, ConceptAsModelCustomizer>()
                .AddInterceptors(new ConceptAsQueryExpressionInterceptor(), new ConceptAsDbCommandInterceptor());

            dbContext = new NamespaceDbContext(builder.Options);
            await dbContext.Database.MigrateAsync();
            namespaces[@namespace.Value] = dbContext;
        }

        return new DbContextScope<NamespaceDbContext>(dbContext, () => namespaces.Remove(key));
    }

    /// <inheritdoc/>
    public Task<DbContextScope<UniqueConstraintDbContext>> UniqueConstraintTable(EventStoreName eventStore, EventStoreNamespaceName @namespace, string constraintName) =>
        GetOrCreateTableDbContext(
            eventStore,
            @namespace,
            constraintName,
            _uniqueConstraintDbContexts,
            (options, name) => new UniqueConstraintDbContext(options, name, uniqueConstraintMigrator));

    /// <inheritdoc/>
    public Task<DbContextScope<EventSequenceDbContext>> EventSequenceTable(EventStoreName eventStore, EventStoreNamespaceName @namespace, string eventSequenceName) =>
        GetOrCreateTableDbContext(
            eventStore,
            @namespace,
            eventSequenceName,
            _eventSequenceDbContexts,
            (options, name) => new EventSequenceDbContext(options, name, eventSequenceMigrator));

    async Task<DbContextScope<TDbContext>> GetOrCreateTableDbContext<TDbContext>(
        EventStoreName eventStore,
        EventStoreNamespaceName @namespace,
        string tableName,
        AsyncLocal<Dictionary<string, Dictionary<string, Dictionary<string, TDbContext>>>> storage,
        Func<DbContextOptions<TDbContext>, string, TDbContext> createDbContext)
        where TDbContext : DbContext, ITableDbContext
    {
        var key = $"{eventStore.Value}:{@namespace.Value}:{tableName}";

        storage.Value ??= new Dictionary<string, Dictionary<string, Dictionary<string, TDbContext>>>();
        if (!storage.Value.TryGetValue(eventStore.Value, out var namespaces))
        {
            namespaces = new Dictionary<string, Dictionary<string, TDbContext>>();
            storage.Value[eventStore.Value] = namespaces;
        }

        if (!namespaces.TryGetValue(@namespace.Value, out var tables))
        {
            tables = new Dictionary<string, TDbContext>();
            namespaces[@namespace.Value] = tables;
        }

        if (!tables.TryGetValue(tableName, out var dbContext))
        {
            var builder = new DbContextOptionsBuilder<TDbContext>();
            var connectionString = GetConnectionStringForEventStoreAndNamespace(eventStore, @namespace);
            builder.UseDatabaseFromConnectionString(connectionString);
            builder
                .UseApplicationServiceProvider(serviceProvider)
                .ReplaceService<IEvaluatableExpressionFilter, ConceptAsEvaluatableExpressionFilter>()
                .ReplaceService<IModelCustomizer, ConceptAsModelCustomizer>()
                .AddInterceptors(new ConceptAsQueryExpressionInterceptor(), new ConceptAsDbCommandInterceptor());

            dbContext = createDbContext(builder.Options, tableName);
            await dbContext.EnsureTableExists();
            tables[tableName] = dbContext;
        }

        return new DbContextScope<TDbContext>(dbContext, () => tables.Remove(tableName));
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
