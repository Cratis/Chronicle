// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using Cratis.Arc.Connections;
using Cratis.Arc.EntityFrameworkCore;
using Cratis.Arc.EntityFrameworkCore.Concepts;
using Cratis.Arc.EntityFrameworkCore.Connections;
using Cratis.Chronicle.Configuration;
using Cratis.Chronicle.Storage.Sql.Cluster;
using Cratis.Chronicle.Storage.Sql.Connections;
using Cratis.Chronicle.Storage.Sql.EventStores;
using Cratis.Chronicle.Storage.Sql.EventStores.Namespaces;
using Cratis.Chronicle.Storage.Sql.Schema;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Cratis.Chronicle.Storage.Sql;

/// <summary>
/// The part of <see cref="Database"/> that decides where and how each database context connects.
/// </summary>
public partial class Database
{
    /// <summary>
    /// How many times an operation is retried when its connection fails, including failed logins once connection
    /// resolution is configured.
    /// </summary>
    const int MaxRetryCount = 6;

    /// <summary>
    /// The longest delay between retries.
    /// </summary>
    static readonly TimeSpan _maxRetryDelay = TimeSpan.FromSeconds(30);

    /// <summary>
    /// Builds the options every database context of the kernel is created with.
    /// </summary>
    /// <typeparam name="TDbContext">Type of database context.</typeparam>
    /// <param name="serviceProvider">The <see cref="IServiceProvider"/> of the application.</param>
    /// <param name="connectionString">The connection string of the database.</param>
    /// <returns>The options.</returns>
    /// <remarks>
    /// Migrations run in the order of their releases (<see cref="ReleaseOrderedMigrations"/>). The migrator is replaced in these shared options
    /// rather than in each context, so every context of a database type keeps sharing one internal EF service provider.
    /// </remarks>
    internal static DbContextOptions<TDbContext> BuildOptions<TDbContext>(IServiceProvider serviceProvider, string connectionString)
        where TDbContext : DbContext => BuildOptions<TDbContext>(serviceProvider, connectionString, serviceProvider.GetService<IOptions<ChronicleOptions>>()?.Value.Storage.SchemaMode ?? SchemaMode.Migrate);

    /// <summary>
    /// Builds options for a resolved target, including its configured authentication provider.
    /// </summary>
    /// <param name="target">The resolved database target.</param>
    /// <param name="schemaMode">An optional schema-policy override for read-only probes.</param>
    /// <typeparam name="TDbContext">The database context type.</typeparam>
    /// <returns>The configured context options.</returns>
    internal DbContextOptions<TDbContext> BuildOptions<TDbContext>(SqlTarget target, SchemaMode? schemaMode = null)
        where TDbContext : DbContext
    {
        var mode = schemaMode ?? options.Value.Storage.SchemaMode;
        var built = BuildOptions<TDbContext>(serviceProvider, target.ConnectionString, mode);
        var builder = new DbContextOptionsBuilder<TDbContext>(built);
        if (target.ConnectionString.GetDatabaseType() == DatabaseType.PostgreSql && _resolution.PostgreSqlEntra is { } authentication)
        {
            builder.UseNpgsql(target.ConnectionString, _ => _.ConfigureDataSource(authentication.Configure));
        }

        if (target.Request is null)
        {
            return builder.Options;
        }

        if (_resolution.PostgreSqlEntra is null || target.ConnectionString.GetDatabaseType() != DatabaseType.PostgreSql)
        {
            builder.AddInterceptors(new ResolvedIdentityInterceptor(_resolution.Resolver, target.Request));
        }

        // A failed login drops the cached resolution, so retrying it resolves the identity again — picking up a rotated
        // password or a refreshed token instead of failing the operation on the first attempt.
        switch (target.ConnectionString.GetDatabaseType())
        {
            case DatabaseType.SqlServer:
                builder.UseSqlServer(target.ConnectionString, _ => _.EnableRetryOnFailure(MaxRetryCount, _maxRetryDelay, [ConnectionResolutionInterceptor.SqlServerLoginFailed]));
                break;

            case DatabaseType.PostgreSql:
                builder.UseNpgsql(target.ConnectionString, _ => _.EnableRetryOnFailure(MaxRetryCount, _maxRetryDelay, [.. ConnectionResolutionInterceptor.PostgreSqlAuthenticationFailed]));
                break;
        }

        return builder.Options;
    }

    static DbContextOptions<TDbContext> BuildOptions<TDbContext>(IServiceProvider serviceProvider, string connectionString, SchemaMode mode)
        where TDbContext : DbContext
    {
        var builder = new DbContextOptionsBuilder<TDbContext>();
        builder.UseDatabaseFromConnectionString(connectionString);
        builder
            .UseApplicationServiceProvider(serviceProvider)
            .AddConceptAsSupport();

        if (connectionString.GetDatabaseType() == DatabaseType.PostgreSql)
        {
            builder.ReplaceService<IMigrator, ReleaseOrderedNpgsqlMigrator>();
        }
        else
        {
            builder.ReplaceService<IMigrator, ReleaseOrderedMigrator>();
        }

        ((IDbContextOptionsBuilderInfrastructure)builder).AddOrUpdateExtension(new SchemaModeOptionsExtension(mode));

        // SQLite serializes writers on a database-wide lock; the kernel writes from many grains at
        // once, so without WAL + a busy timeout concurrent writes fail with SQLITE_BUSY and stall
        // observer catch-up. Verification skips it because changing journal_mode writes to the file.
        if (connectionString.GetDatabaseType() == DatabaseType.Sqlite && mode != SchemaMode.Verify)
        {
            builder.AddInterceptors(SqlitePragmaConnectionInterceptor.Instance);
        }

        SqlMigrationTelemetry.Configure(builder, connectionString, serviceProvider.GetService<ILogger<Database>>() ?? NullLogger<Database>.Instance);
        return builder.Options;
    }

    /// <summary>
    /// Resolve where a database is and how to connect to it for a purpose.
    /// </summary>
    /// <param name="identity">The <see cref="DatabaseIdentity"/>.</param>
    /// <param name="purpose">The <see cref="ConnectionPurpose"/>.</param>
    /// <param name="readOnly">Whether binding resolution must avoid writes.</param>
    /// <returns>The <see cref="SqlTarget"/>.</returns>
    /// <remarks>
    /// When the resolution places the database — with a connection string or a database name — that placement is used
    /// as it is. Otherwise the database is derived from the configured connection string, through the permanent
    /// bindings where the provider uses them. The cluster database is always the configured one; only its identity and
    /// schema are resolved.
    /// </remarks>
    async Task<SqlTarget> Target(DatabaseIdentity identity, ConnectionPurpose purpose, bool readOnly = false)
    {
        if (identity.Kind == DatabaseKind.Cluster)
        {
            return ClusterTarget(purpose);
        }

        var resolved = await _resolution.Resolve(identity, purpose);
        var connectionString = SqlConnectionStrings.OwnsPlacement(resolved)
            ? SqlConnectionStrings.WithPlacement(options.Value.Storage.ConnectionDetails, resolved)
            : await ResolveConnectionString(identity, readOnly);

        return TargetFor(identity, purpose, connectionString, resolved);
    }

    SqlTarget ClusterTarget(ConnectionPurpose purpose)
    {
        // Also used where the resolution cannot be awaited. It is cached, so this only waits when it is not.
        var resolved = _resolution.Resolve(DatabaseIdentity.Cluster, purpose).GetAwaiter().GetResult();
        return TargetFor(DatabaseIdentity.Cluster, purpose, options.Value.Storage.ConnectionDetails, resolved);
    }

    SqlTarget TargetFor(DatabaseIdentity identity, ConnectionPurpose purpose, string connectionString, ResolvedConnection resolved)
    {
        var schema = _resolution.SchemaFor(resolved);
        return new(
            SqlConnectionStrings.WithSchema(connectionString, schema),
            schema,
            _resolution.IsConfigured ? _resolution.RequestFor(identity, purpose) : null);
    }

    /// <summary>
    /// Get options for a database context, rebuilding them only when where the context connects changes.
    /// </summary>
    /// <typeparam name="TDbContext">The type of database context.</typeparam>
    /// <param name="cache">The cache to keep the options in.</param>
    /// <param name="key">The key — what the context is for and for which purpose, never the credential.</param>
    /// <param name="target">The <see cref="SqlTarget"/>.</param>
    /// <returns>The options.</returns>
    DbContextOptions<TDbContext> OptionsFor<TDbContext>(ConcurrentDictionary<string, CachedDbContextOptions<TDbContext>> cache, string key, SqlTarget target)
        where TDbContext : DbContext
    {
        if (cache.TryGetValue(key, out var cached) && cached.ConnectionString == target.ConnectionString)
        {
            return cached.Options;
        }

        var built = new CachedDbContextOptions<TDbContext>(target.ConnectionString, BuildOptions<TDbContext>(target));
        cache[key] = built;
        return built.Options;
    }

    /// <summary>
    /// Create a database context for data access, after preparing its database and tables.
    /// </summary>
    /// <typeparam name="TDbContext">The type of database context.</typeparam>
    /// <param name="identity">The <see cref="DatabaseIdentity"/>.</param>
    /// <param name="key">The cache key for what the context is for.</param>
    /// <param name="cache">The options cache.</param>
    /// <param name="create">Creates a context from options.</param>
    /// <param name="prepare">Creates or migrates what the context needs, given a context and where it connects.</param>
    /// <param name="purpose">The <see cref="ConnectionPurpose"/> of the context that is returned.</param>
    /// <returns>The context. The caller owns it.</returns>
    /// <remarks>
    /// With connection resolution configured, the preparation runs on its own short-lived context connected for
    /// <see cref="ConnectionPurpose.Schema"/>, so a runtime identity never needs the right to change the schema.
    /// </remarks>
    async Task<TDbContext> CreateContext<TDbContext>(
        DatabaseIdentity identity,
        string key,
        ConcurrentDictionary<string, CachedDbContextOptions<TDbContext>> cache,
        Func<DbContextOptions<TDbContext>, TDbContext> create,
        Func<TDbContext, SqlTarget, Task> prepare,
        ConnectionPurpose purpose = ConnectionPurpose.Runtime)
        where TDbContext : DbContext
    {
        var target = await Target(identity, purpose);
        var context = create(OptionsFor(cache, $"{key}:{purpose}", target));
        try
        {
            if (!_resolution.IsConfigured || purpose == ConnectionPurpose.Schema)
            {
                await prepare(context, target);
                return context;
            }

            var schemaTarget = await Target(identity, ConnectionPurpose.Schema);
            await using var schemaContext = create(OptionsFor(cache, $"{key}:{nameof(ConnectionPurpose.Schema)}", schemaTarget));
            await prepare(schemaContext, schemaTarget);
            return context;
        }
        catch
        {
            await context.DisposeAsync();
            throw;
        }
    }

    Task<string> ResolveConnectionString(DatabaseIdentity identity, bool readOnly = false)
    {
        if (UsesBindings && (VerifySchema || readOnly))
        {
            return Task.FromResult(KnownConnectionString(identity));
        }

        return UsesBindings
        ? _databaseNames.Resolve(
            identity,
            Cluster,
            connection => new EventStoreDbContext(BuildOptions<EventStoreDbContext>(MetadataTarget(connection))),
            connection => new NamespaceDbContext(BuildOptions<NamespaceDbContext>(MetadataTarget(connection))),
            serviceProvider.GetService<ILogger<DatabaseNameResolver>>() ?? NullLogger<DatabaseNameResolver>.Instance)
        : Task.FromResult(ConnectionStringFor(identity.LogicalName(string.Empty)));
    }

    string KnownConnectionString(DatabaseIdentity identity)
    {
        try
        {
            return _databaseNames.Known(identity, () => new ClusterDbContext(BuildOptions<ClusterDbContext>(ClusterTarget(ConnectionPurpose.Schema))));
        }
        catch (DatabaseNameBindingFailure)
        {
            throw new SchemaNotCurrent(identity.LogicalName(string.Empty), identity.ToString(), ["database binding missing"]);
        }
    }

    /// <summary>
    /// Target a candidate database the binding inspects before it is bound, as the cluster's schema identity.
    /// </summary>
    /// <param name="connectionString">The candidate's connection string.</param>
    /// <returns>The <see cref="SqlTarget"/>.</returns>
    SqlTarget MetadataTarget(string connectionString) =>
        new(connectionString, options.Value.Storage.Schema, _resolution.IsConfigured ? _resolution.RequestFor(DatabaseIdentity.Cluster, ConnectionPurpose.Schema) : null);
}
