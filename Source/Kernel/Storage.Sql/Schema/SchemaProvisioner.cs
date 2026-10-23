// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Storage.Sql.Cluster;
using Cratis.Chronicle.Storage.Sql.EventStores;
using Cratis.Chronicle.Storage.Sql.EventStores.Namespaces;
using Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.EventSequences;
using Cratis.Orleans.Storage.Sql.Jobs;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Storage.Sql.Schema;

/// <summary>
/// Visits every known SQL scope using the schema connection identity.
/// </summary>
/// <param name="database">The SQL database.</param>
/// <param name="logger">The schema logger.</param>
public class SchemaProvisioner(Database database, ILogger<SchemaProvisioner> logger) : ISchemaProvisioner
{
    static readonly string[] _sequences = [WellKnownEventSequences.EventLog, WellKnownEventSequences.System, WellKnownEventSequences.Outbox];

    /// <inheritdoc/>
    public Task<SchemaReport> Migrate(SchemaScope scope) => Run(scope, false);

    /// <inheritdoc/>
    public Task<SchemaReport> Verify(SchemaScope scope) => Run(scope, true);

    async Task<SchemaReport> Run(SchemaScope scope, bool verify)
    {
        var results = new List<SchemaScopeResult>();
        await Visit<ClusterDbContext>(DatabaseIdentity.Cluster, static opts => new ClusterDbContext(opts), verify, results, async cluster =>
        {
            var stores = (await cluster.EventStores.Select(store => store.Name).ToListAsync())
                .Concat(scope.EventStores).Append(EventStoreName.System.Value).Distinct(StringComparer.Ordinal);
            foreach (var store in stores)
            {
                await VisitStore(store, scope, verify, results);
                if (!verify && !await cluster.EventStores.AnyAsync(row => row.Name == store))
                {
                    cluster.EventStores.Add(new EventStore { Name = store });
                    await cluster.SaveChangesAsync();
                }
            }
        });
        return new(results);
    }

    Task VisitStore(string store, SchemaScope scope, bool verify, List<SchemaScopeResult> results) =>
        Visit<EventStoreDbContext>(DatabaseIdentity.Store(store), static opts => new EventStoreDbContext(opts), verify, results, async context =>
        {
            var namespaces = (await context.Namespaces.Select(ns => ns.Name).ToListAsync())
                .Concat(scope.EventStores.Contains(store, StringComparer.Ordinal) ? scope.Namespaces : []).Append(EventStoreNamespaceName.Default.Value).Distinct(StringComparer.Ordinal);
            foreach (var ns in namespaces)
            {
                var identity = new DatabaseIdentity(DatabaseKind.Namespace, store, ns);
                await Visit<NamespaceDbContext>(identity, static opts => new NamespaceDbContext(opts), verify, results, ctx => PrepareNamespace(identity, ctx, verify, results));
                await Visit<JobsDbContext>(identity, static opts => new JobsDbContext(opts), verify, results, _ => Task.CompletedTask);
                if (!verify && !await context.Namespaces.AnyAsync(row => row.Name == ns))
                {
                    context.Namespaces.Add(new Namespace { Name = ns, Created = DateTimeOffset.UtcNow });
                    await context.SaveChangesAsync();
                }
            }
        });

    async Task PrepareNamespace(DatabaseIdentity identity, NamespaceDbContext context, bool verify, List<SchemaScopeResult> results)
    {
        var tables = await SchemaTables.Read(context);
        if (verify)
        {
            var missing = _sequences.Append(EventSequenceMigrator.NamedTagsTable).Where(name => !tables.ContainsKey(name)).ToArray();
            results.Add(new($"{identity}:sequences", context.Database.GetDbConnection().Database, missing, []));
            if (missing.Length != 0)
            {
                SchemaProvisionerLogging.ScopePending(logger, identity.ToString(), context.Database.GetDbConnection().Database, string.Join(", ", missing));
            }
        }
        else
        {
            await database.PrepareSequences(identity, _sequences.Concat(tables.Where(table => SchemaTables.IsSequence(table.Value)).Select(table => table.Key)).Distinct(StringComparer.Ordinal));
        }

        try
        {
            var name = await database.PrepareReadModels(identity, verify);
            results.Add(new($"{identity}:read-models", name, [], []));
        }
        catch (SchemaNotCurrent error) when (verify)
        {
            results.Add(new($"{identity}:read-models", error.Database, error.Pending, []));
        }
    }

    async Task Visit<TContext>(DatabaseIdentity identity, Func<DbContextOptions<TContext>, TContext> create, bool verify, List<SchemaScopeResult> results, Func<TContext, Task> visit)
        where TContext : DbContext
    {
        var scope = $"{identity}:{typeof(TContext).Name}";
        var timer = Stopwatch.StartNew();
        try
        {
            await using var context = await database.SchemaContext(identity, create, verify);
            var applied = (await context.Database.GetAppliedMigrationsAsync()).ToArray();
            var unknown = applied.Except(context.Database.GetMigrations(), StringComparer.Ordinal).ToArray();
            var name = context.Database.GetDbConnection().Database;
            results.Add(new(scope, name, [], unknown));
            SchemaProvisionerLogging.ScopeCurrent(logger, scope, name, timer.Elapsed);
            if (unknown.Length != 0)
            {
                SchemaProvisionerLogging.NewerMigrationsPresent(logger, name, string.Join(", ", unknown));
            }

            await visit(context);
        }
        catch (SchemaNotCurrent error) when (verify)
        {
            results.Add(new(scope, error.Database, error.Pending, []));
            SchemaProvisionerLogging.ScopePending(logger, scope, error.Database, string.Join(", ", error.Pending));
        }
    }
}
