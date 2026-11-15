// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using Cratis.Arc.EntityFrameworkCore;
using Cratis.Chronicle.Concepts.Sinks;
using Cratis.Chronicle.Configuration;
using Cratis.Chronicle.Storage.Sinks;
using Cratis.Chronicle.Storage.Sql.Connections;
using Cratis.Chronicle.Storage.Sql.EventStores.Namespaces;
using Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.EventSequences;
using Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.ReadModels;
using Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.UniqueConstraints;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Cratis.Chronicle.Storage.Sql.Sinks;

/// <summary>
/// Represents the SQL databases of named sink configurations, independent of the kernel's event storage.
/// </summary>
/// <remarks>
/// A named SQL sink connects to its own server, so read models can be stored in PostgreSQL, Microsoft SQL Server or SQLite whatever
/// engine the kernel keeps its events in. The engine is read from the connection string, and each configuration gets its own
/// <see cref="Database"/>, built from the kernel's storage settings with the configuration's connection in their place.
/// </remarks>
/// <param name="serviceProvider">The <see cref="IServiceProvider"/> of the kernel.</param>
public class NamedSinkDatabases(IServiceProvider serviceProvider)
{
    readonly ConcurrentDictionary<string, (IDatabase Database, ReplayingTables ReplayingTables)> _databases = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Get the database of a named sink, and the tables being replayed in it.
    /// </summary>
    /// <param name="sink">The <see cref="SinkName"/> of the configuration.</param>
    /// <returns>The <see cref="IDatabase"/> and its <see cref="ReplayingTables"/>.</returns>
    /// <exception cref="SinkTypeDoesNotSupportNamedConfigurations">Thrown when the named configuration is not a SQL sink.</exception>
    public (IDatabase Database, ReplayingTables ReplayingTables) Get(SinkName sink)
    {
        var options = serviceProvider.GetRequiredService<IOptions<ChronicleOptions>>().Value;
        if (!options.Sinks.TryGet(sink.Value, out var key, out var configuration) ||
            !string.Equals(configuration.Type, WellKnownSinkTypes.SQL.Value, StringComparison.OrdinalIgnoreCase))
        {
            throw new SinkTypeDoesNotSupportNamedConfigurations(WellKnownSinkTypes.SQL, sink);
        }

        return _databases.GetOrAdd(key, static (_, state) => (state.Self.Create(state.Options, state.Configuration), new ReplayingTables()), (Self: this, Options: options, Configuration: configuration));
    }

    /// <summary>
    /// Get the storage settings of a named SQL sink: the kernel's own, with the configuration's connection in their place.
    /// </summary>
    /// <param name="kernel">The <see cref="ChronicleOptions"/> of the kernel.</param>
    /// <param name="configuration">The <see cref="SinkConfiguration"/> of the sink.</param>
    /// <returns>The <see cref="ChronicleOptions"/> the sink's <see cref="Database"/> is built with.</returns>
    internal static ChronicleOptions OptionsFor(ChronicleOptions kernel, SinkConfiguration configuration) => new()
    {
        Storage = new()
        {
            Type = StorageTypeFor(configuration.ConnectionDetails),
            ConnectionDetails = configuration.ConnectionDetails,
            DatabaseNamePrefix = kernel.Storage.DatabaseNamePrefix,
            SystemDatabaseName = kernel.Storage.SystemDatabaseName,
            CreateIfMissing = kernel.Storage.CreateIfMissing,
            SchemaMode = kernel.Storage.SchemaMode,
            PostgreSqlPooling = kernel.Storage.PostgreSqlPooling,
            ConnectionResolution = kernel.Storage.ConnectionResolution
        },
        Sql = kernel.Sql,
        Telemetry = kernel.Telemetry,
        Features = kernel.Features,
        ReadModels = kernel.ReadModels,
        Sinks = kernel.Sinks
    };

    /// <summary>
    /// Get the storage type a SQL connection string connects to.
    /// </summary>
    /// <param name="connectionDetails">The connection string.</param>
    /// <returns>One of <see cref="StorageType"/>.</returns>
    internal static string StorageTypeFor(string connectionDetails) => connectionDetails.GetDatabaseType() switch
    {
        DatabaseType.PostgreSql => StorageType.PostgreSql,
        DatabaseType.SqlServer => StorageType.MsSql,
        _ => StorageType.Sqlite
    };

    Database Create(ChronicleOptions kernel, SinkConfiguration configuration)
    {
        var options = Options.Create(OptionsFor(kernel, configuration));
        var readModelMigrator = new ReadModelMigrator(new TableMigrator<ReadModelDbContext>(Logger<TableMigrator<ReadModelDbContext>>(), options), Logger<ReadModelMigrator>(), options);
        var provider = new NamedSinkServiceProvider(serviceProvider, options, readModelMigrator);
        return new Database(
            provider,
            options,
            new EventSequenceMigrator(new TableMigrator<EventSequenceDbContext>(Logger<TableMigrator<EventSequenceDbContext>>(), options), Logger<EventSequenceMigrator>()),
            new UniqueConstraintMigrator(
                new TableMigrator<UniqueConstraintDbContext>(Logger<TableMigrator<UniqueConstraintDbContext>>(), options),
                new TableMigrator<UniqueConstraintValuesDbContext>(Logger<TableMigrator<UniqueConstraintValuesDbContext>>(), options),
                Logger<UniqueConstraintMigrator>()),
            readModelMigrator);
    }

    ILogger<T> Logger<T>() => serviceProvider.GetRequiredService<ILogger<T>>();

    /// <summary>
    /// Serves a named sink's <see cref="Database"/> its own settings and migrator, and hides the kernel's connection resolution so the
    /// sink resolves connections for its own engine.
    /// </summary>
    /// <param name="inner">The kernel's <see cref="IServiceProvider"/>.</param>
    /// <param name="options">The sink's <see cref="ChronicleOptions"/>.</param>
    /// <param name="readModelMigrator">The sink's <see cref="IReadModelMigrator"/>.</param>
    sealed class NamedSinkServiceProvider(IServiceProvider inner, IOptions<ChronicleOptions> options, IReadModelMigrator readModelMigrator) : IServiceProvider
    {
        public object? GetService(Type serviceType)
        {
            if (serviceType == typeof(IOptions<ChronicleOptions>)) return options;
            if (serviceType == typeof(IReadModelMigrator)) return readModelMigrator;
            if (serviceType == typeof(SqlConnectionResolution) ||
                serviceType == typeof(IStorageConnectionResolution) ||
                serviceType == typeof(Choreography.Storage.IJobsStorage))
            {
                return null;
            }

            return inner.GetService(serviceType);
        }
    }
}
