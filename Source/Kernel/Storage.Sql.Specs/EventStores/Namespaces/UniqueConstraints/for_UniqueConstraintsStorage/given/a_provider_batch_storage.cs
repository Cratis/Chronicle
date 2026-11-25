// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.EntityFrameworkCore.Concepts;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Events.Constraints;
using Cratis.Chronicle.Storage.Events.Constraints;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.UniqueConstraints.for_UniqueConstraintsStorage.given;

public abstract class a_provider_batch_storage : Specification
{
    protected UniqueConstraintsStorage _storage;
    protected UniqueConstraintsStorage _oracle;
    protected UniqueConstraintDefinition _definition;
    protected readonly List<string> _scopes = [string.Empty, "other"];
    protected readonly List<IInterceptor> _interceptors = [];
    protected readonly List<DbContext> _batchContexts = [];
    protected int _disposedBatchContexts;
    protected bool _sqlServer;
    readonly List<(IReadOnlyList<(bool, EventSequenceNumber)> Actual, IReadOnlyList<(bool, EventSequenceNumber)> Expected)> _claimResults = [];
    readonly List<(int[] Actual, int[] Expected)> _refusedResults = [];
    readonly List<(string[] Actual, string[] Expected)> _storedResults = [];
    Action<DbContextOptionsBuilder> _configure;
    Func<Task> _drop = () => Task.CompletedTask;
    IUniqueConstraintMigrator _migrator;
    IDatabase _database;
    int _preparations;
    EventStoreNamespaceName _batchNamespace;
    EventStoreNamespaceName _oracleNamespace;

    protected void Configure(Action<DbContextOptionsBuilder> configure, bool sqlServer, Func<Task> drop)
    {
        _configure = configure;
        _sqlServer = sqlServer;
        _drop = drop;
        _migrator = new UniqueConstraintMigrator(
            new TableMigrator<UniqueConstraintDbContext>(Substitute.For<ILogger<TableMigrator<UniqueConstraintDbContext>>>()),
            new TableMigrator<UniqueConstraintValuesDbContext>(Substitute.For<ILogger<TableMigrator<UniqueConstraintValuesDbContext>>>()),
            Substitute.For<ILogger<UniqueConstraintMigrator>>());
        _database = Substitute.For<IDatabase>();
        _database.UniqueConstraintTable(Arg.Any<EventStoreName>(), Arg.Any<EventStoreNamespaceName>(), Arg.Any<string>())
            .Returns(call => SourceScope(call.ArgAt<EventStoreNamespaceName>(1), call.ArgAt<string>(2)));
        _database.UniqueConstraintValuesTable(Arg.Any<EventStoreName>(), Arg.Any<EventStoreNamespaceName>(), Arg.Any<string>())
            .Returns(call => ValueScope(call.ArgAt<EventStoreNamespaceName>(1), call.ArgAt<string>(2)));
    }

    Task Destroy() => _drop();

    protected async Task Prepare(UniqueConstraintMode mode)
    {
        _batchNamespace = $"b{++_preparations}";
        _oracleNamespace = $"o{_preparations}";
        _storage = new("batch-store", _batchNamespace, "sequence", _database);
        _oracle = new("batch-store", _oracleNamespace, "sequence", _database);
        _definition = new("constraint", []) { Mode = mode };
        foreach (var ns in new[] { _batchNamespace, _oracleNamespace })
        {
            foreach (var key in _scopes)
            {
                var prefix = string.IsNullOrEmpty(key) ? "sequence_constraint" : $"sequence_constraint_{key}";
                await using var sources = await SourceScope(ns, $"{prefix}_constraint");
                await using var values = await ValueScope(ns, $"{prefix}_constraint_values");
            }
        }
    }

    protected async Task Seed()
    {
        foreach (var storage in new[] { _storage, _oracle })
        {
            await storage.Save("owner", _definition, 10UL, "held", subject: "original");
            await storage.Save("moving", _definition, 11UL, "old");
            await storage.Save("released", _definition, 12UL, "release");
        }
    }

    protected async Task CheckClaims(IReadOnlyList<UniqueConstraintClaim> claims)
    {
        var actual = await _storage.IsAllowedMany(claims, _definition);
        var expected = new List<(bool, EventSequenceNumber)>();
        foreach (var claim in claims) expected.Add(await _oracle.IsAllowed(claim.EventSourceId, _definition, claim.Value));
        _claimResults.Add((actual, expected));
    }

    protected async Task CheckChanges(IReadOnlyList<UniqueConstraintIndexChange> changes)
    {
        var expected = await UniqueConstraintIndexChanges.ApplySequentially(_oracle, _definition, changes);
        var actual = await _storage.ApplyChanges(_definition, changes);
        _refusedResults.Add((actual.Order().ToArray(), expected.Order().ToArray()));
        await CompareSnapshots();
    }

    protected async Task CompareSnapshots()
    {
        foreach (var key in _scopes) _storedResults.Add((await Snapshot(false, key), await Snapshot(true, key)));
    }

    protected void VerifyClaims()
    {
        foreach (var (actual, expected) in _claimResults) actual.ShouldEqual(expected);
    }

    protected void VerifyChanges()
    {
        foreach (var (actual, expected) in _refusedResults) actual.ShouldEqual(expected);
        foreach (var (actual, expected) in _storedResults) actual.ShouldEqual(expected);
    }

    protected static UniqueConstraintIndexChange Save(EventSourceId source, UniqueConstraintValue value, EventSequenceNumber? sequence = null, string scope = "", Subject? subject = null) =>
        new(UniqueConstraintIndexChangeKind.Save, source, sequence ?? (EventSequenceNumber)20UL, scope, value, subject);

    protected static UniqueConstraintIndexChange Remove(EventSourceId source, string scope = "") =>
        new(UniqueConstraintIndexChangeKind.Remove, source, 0UL, scope);

    protected static UniqueConstraintIndexChange RemoveValue(EventSourceId source, UniqueConstraintValue value) =>
        new(UniqueConstraintIndexChangeKind.RemoveValue, source, 0UL, "", value);

    protected async Task<string[]> Snapshot(bool oracle = false, string key = "")
    {
        var ns = oracle ? _oracleNamespace : _batchNamespace;
        var prefix = string.IsNullOrEmpty(key) ? "sequence_constraint" : $"sequence_constraint_{key}";
        await using var sources = await SourceScope(ns, $"{prefix}_constraint");
        await using var values = await ValueScope(ns, $"{prefix}_constraint_values");
        var sourceRows = await sources.DbContext.Entries.AsNoTracking().ToArrayAsync();
        var valueRows = await values.DbContext.Entries.AsNoTracking().ToArrayAsync();
        return sourceRows.Select(row => $"source:{row.EventSourceId}:{row.Value}:{row.SequenceNumber.ToString("G29", System.Globalization.CultureInfo.InvariantCulture)}:{row.Subject}")
            .Concat(valueRows.Select(row => $"value:{row.EventSourceId}:{row.Value}:{row.SequenceNumber.ToString("G29", System.Globalization.CultureInfo.InvariantCulture)}:{row.Subject}"))
            .Order().ToArray();
    }

    async Task<DbContextScope<UniqueConstraintDbContext>> SourceScope(EventStoreNamespaceName ns, string table)
    {
        var context = new UniqueConstraintDbContext(Options<UniqueConstraintDbContext>(ns), $"{ns}_{table}", _migrator);
        await context.EnsureTableExists();
        return Track(ns, context);
    }

    async Task<DbContextScope<UniqueConstraintValuesDbContext>> ValueScope(EventStoreNamespaceName ns, string table)
    {
        var context = new UniqueConstraintValuesDbContext(Options<UniqueConstraintValuesDbContext>(ns), $"{ns}_{table}", _migrator);
        await context.EnsureTableExists();
        return Track(ns, context);
    }

    DbContextOptions<T> Options<T>(EventStoreNamespaceName ns)
        where T : DbContext
    {
        var builder = new DbContextOptionsBuilder<T>();
        _configure(builder);
        builder.AddConceptAsSupport().WithoutServiceProviderCaching();
        if (ns == _batchNamespace) builder.AddInterceptors(_interceptors);
        return builder.Options;
    }

    DbContextScope<T> Track<T>(EventStoreNamespaceName ns, T context)
        where T : DbContext
    {
        if (ns != _batchNamespace) return new(context, () => { });
        _batchContexts.Add(context);
        return new(context, () => _disposedBatchContexts++);
    }
}
