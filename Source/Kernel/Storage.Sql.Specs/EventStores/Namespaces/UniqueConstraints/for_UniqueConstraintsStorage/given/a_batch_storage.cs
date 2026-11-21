// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Events.Constraints;
using Cratis.Chronicle.Storage.Events.Constraints;
using Cratis.Chronicle.Storage.Sql.for_Database.given;
using Microsoft.EntityFrameworkCore;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.UniqueConstraints.for_UniqueConstraintsStorage.given;

public class a_batch_storage : a_sqlite_database
{
    protected UniqueConstraintsStorage _storage;
    protected UniqueConstraintsStorage _oracle;
    protected UniqueConstraintDefinition _definition;
    protected readonly List<string> _scopes = [string.Empty, "other"];

    readonly List<(IReadOnlyList<(bool, EventSequenceNumber)> Actual, IReadOnlyList<(bool, EventSequenceNumber)> Expected)> _claimResults = [];
    readonly List<(int[] Actual, int[] Expected)> _refusedResults = [];
    readonly List<(string[] Actual, string[] Expected)> _storedResults = [];
    EventStoreNamespaceName _batchNamespace;
    EventStoreNamespaceName _oracleNamespace;

    protected async Task Prepare(UniqueConstraintMode mode)
    {
        _batchNamespace = $"batch-{mode}";
        _oracleNamespace = $"oracle-{mode}";
        _storage = new("batch-store", _batchNamespace, "sequence", _database);
        _oracle = new("batch-store", _oracleNamespace, "sequence", _database);
        _definition = new("constraint", []) { Mode = mode };
        foreach (var ns in new[] { _batchNamespace, _oracleNamespace })
        {
            foreach (var key in _scopes)
            {
                var prefix = string.IsNullOrEmpty(key) ? "sequence_constraint" : $"sequence_constraint_{key}";
                await using var sources = await _database.UniqueConstraintTable("batch-store", ns, $"{prefix}_constraint");
                await sources.DbContext.Database.ExecuteSqlRawAsync(sources.DbContext.Database.GenerateCreateScript());
                await using var values = await _database.UniqueConstraintValuesTable("batch-store", ns, $"{prefix}_constraint_values");
                await values.DbContext.Database.ExecuteSqlRawAsync(values.DbContext.Database.GenerateCreateScript());
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
        foreach (var claim in claims)
        {
            expected.Add(await _oracle.IsAllowed(claim.EventSourceId, _definition, claim.Value));
        }
        _claimResults.Add((actual, expected));
    }

    protected async Task CheckChanges(IReadOnlyList<UniqueConstraintIndexChange> changes)
    {
        var expected = await UniqueConstraintIndexChanges.ApplySequentially(_oracle, _definition, changes);
        var actual = await _storage.ApplyChanges(_definition, changes);
        _refusedResults.Add((actual.Order().ToArray(), expected.Order().ToArray()));
        foreach (var key in _scopes)
        {
            _storedResults.Add((await Snapshot(_batchNamespace, key), await Snapshot(_oracleNamespace, key)));
        }
    }

    protected void VerifyClaims()
    {
        foreach (var (actual, expected) in _claimResults)
        {
            actual.ShouldEqual(expected);
        }
    }

    protected void VerifyChanges()
    {
        foreach (var (actual, expected) in _refusedResults)
        {
            actual.ShouldEqual(expected);
        }
        foreach (var (actual, expected) in _storedResults)
        {
            actual.ShouldEqual(expected);
        }
    }

    protected static UniqueConstraintIndexChange Save(EventSourceId source, UniqueConstraintValue value, EventSequenceNumber? sequence = null, string scope = "", Subject? subject = null) =>
        new(UniqueConstraintIndexChangeKind.Save, source, sequence ?? (EventSequenceNumber)20UL, scope, value, subject);

    protected static UniqueConstraintIndexChange Remove(EventSourceId source, string scope = "") =>
        new(UniqueConstraintIndexChangeKind.Remove, source, 0UL, scope);

    protected static UniqueConstraintIndexChange RemoveValue(EventSourceId source, UniqueConstraintValue value) =>
        new(UniqueConstraintIndexChangeKind.RemoveValue, source, 0UL, "", value);

    async Task<string[]> Snapshot(EventStoreNamespaceName ns, string key)
    {
        var prefix = string.IsNullOrEmpty(key) ? "sequence_constraint" : $"sequence_constraint_{key}";
        await using var sources = await _database.UniqueConstraintTable("batch-store", ns, $"{prefix}_constraint");
        await using var values = await _database.UniqueConstraintValuesTable("batch-store", ns, $"{prefix}_constraint_values");
        var sourceRows = await sources.DbContext.Entries.AsNoTracking().ToArrayAsync();
        var valueRows = await values.DbContext.Entries.AsNoTracking().ToArrayAsync();
        return sourceRows.Select(row => $"source:{row.EventSourceId}:{row.Value}:{row.SequenceNumber}:{row.Subject}")
            .Concat(valueRows.Select(row => $"value:{row.EventSourceId}:{row.Value}:{row.SequenceNumber}:{row.Subject}"))
            .Order().ToArray();
    }
}
