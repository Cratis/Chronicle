// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Data.Common;
using System.Diagnostics.CodeAnalysis;
using Cratis.Applications.EntityFrameworkCore;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Configuration;
using Cratis.Chronicle.Storage.Sql.Cluster;
using Cratis.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Cratis.Chronicle.Storage.Sql;

/// <summary>
/// Represents an implementation of <see cref="IDatabase"/>.
/// </summary>
/// <param name="options">The <see cref="IOptions{ChronicleOptions}"/>.</param>
[Singleton]
public class Database(IOptions<ChronicleOptions> options) : IDatabase
{
    readonly AsyncLocal<ClusterDbContext> _clusterDbContext = new();
    readonly AsyncLocal<Dictionary<string, EventStoreDbContext>> _eventStoreDbContexts = new();

    /// <inheritdoc/>
    public async Task<DbContextScope<ClusterDbContext>> Cluster()
    {
        if (_clusterDbContext.Value == null)
        {
            var builder = new DbContextOptionsBuilder<ClusterDbContext>();
            builder.UseDatabaseFromConnectionString(options.Value.Storage.ConnectionDetails);
            var context = new ClusterDbContext(builder.Options);
            await context.Database.MigrateAsync();
            _clusterDbContext.Value = context;
        }

        return new DbContextScope<ClusterDbContext>(_clusterDbContext.Value, () => _clusterDbContext.Value = null!);
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
            dbContext = new EventStoreDbContext(builder.Options);
            await dbContext.Database.MigrateAsync();
            _eventStoreDbContexts.Value[eventStore.Value] = dbContext;
        }

        return new DbContextScope<EventStoreDbContext>(dbContext, () => _eventStoreDbContexts.Value?.Remove(eventStore.Value));
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

    string ReplaceFilename(EventStoreName eventStore)
    {
        if (TryReplaceFilename(eventStore, "Data Source", out var dataSource))
        {
            return dataSource;
        }
        if (TryReplaceFilename(eventStore, "Filename", out var filename))
        {
            return filename;
        }

        return options.Value.Storage.ConnectionDetails;
    }

    bool TryReplaceFilename(EventStoreName eventStore, string keyToReplace, [NotNullWhen(true)] out string? connectionString)
    {
        var builder = new DbConnectionStringBuilder
        {
            ConnectionString = options.Value.Storage.ConnectionDetails
        };

        if (builder.TryGetValue(keyToReplace, out var dataSource))
        {
            var originalFilename = dataSource.ToString()!;
            var directory = Path.GetDirectoryName(originalFilename) ?? string.Empty;
            var newFilename = $"{Path.GetFileNameWithoutExtension(originalFilename)}_{eventStore.Value}{Path.GetExtension(originalFilename)}";
            builder[keyToReplace] = Path.Combine(directory, newFilename);
            connectionString = builder.ConnectionString!;
            return true;
        }

        connectionString = null!;
        return false;
    }
}
