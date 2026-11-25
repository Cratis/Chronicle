// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.EntityFrameworkCore.Concepts;
using Cratis.Chronicle.Storage.Sql;
using Cratis.Chronicle.Storage.Sql.EventStores.Namespaces;
using Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.EventSequences;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Cratis.Chronicle.Benchmarks.Storage.Hosts;

/// <summary>Immutable provider/interceptor configuration built once per host, never per operation.</summary>
/// <param name="provider">The SQL provider.</param>
/// <param name="connectionString">The host's connection string.</param>
/// <param name="interceptors">The host's counting and capture interceptors.</param>
internal sealed class SqlBenchmarkOptions(StorageProvider provider, string connectionString, IInterceptor[] interceptors)
{
    /// <summary>Gets the event-sequence options.</summary>
    public DbContextOptions<EventSequenceDbContext> Events { get; } = Build<EventSequenceDbContext>(provider, connectionString, interceptors);

    /// <summary>Gets the constraint options.</summary>
    public DbContextOptions<UniqueConstraintDbContext> Constraints { get; } = Build<UniqueConstraintDbContext>(provider, connectionString, interceptors);

    /// <summary>Gets the constraint-value options.</summary>
    public DbContextOptions<UniqueConstraintValuesDbContext> Values { get; } = Build<UniqueConstraintValuesDbContext>(provider, connectionString, interceptors);

    /// <summary>Gets the namespace options.</summary>
    public DbContextOptions<NamespaceDbContext> Namespace { get; } = Build<NamespaceDbContext>(provider, connectionString, interceptors);

    /// <summary>Builds immutable options for a context type.</summary>
    /// <typeparam name="T">The database context type.</typeparam>
    /// <param name="provider">The SQL provider.</param>
    /// <param name="connectionString">The host's connection string.</param>
    /// <param name="interceptors">The host's counting and capture interceptors.</param>
    /// <returns>The configured context options.</returns>
    /// <exception cref="ArgumentOutOfRangeException">The provider is not a supported SQL provider.</exception>
    internal static DbContextOptions<T> Build<T>(StorageProvider provider, string connectionString, IInterceptor[] interceptors)
        where T : DbContext
    {
        var builder = new DbContextOptionsBuilder<T>();
        _ = provider switch
        {
            StorageProvider.Sqlite => builder.UseSqlite(connectionString),
            StorageProvider.PostgreSql => builder.UseNpgsql(connectionString),
            StorageProvider.SqlServer => builder.UseSqlServer(connectionString),
            _ => throw new ArgumentOutOfRangeException(nameof(provider))
        };
        builder.EnableServiceProviderCaching(true).AddConceptAsSupport().AddInterceptors(interceptors);
        if (typeof(T) == typeof(NamespaceDbContext))
        {
            if (provider == StorageProvider.PostgreSql) builder.ReplaceService<IMigrator, ReleaseOrderedNpgsqlMigrator>();
            else builder.ReplaceService<IMigrator, ReleaseOrderedMigrator>();
        }
        return builder.Options;
    }
}
