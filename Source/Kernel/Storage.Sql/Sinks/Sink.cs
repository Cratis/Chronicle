// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using System.Text.Json.Nodes;
using Cratis.Chronicle.Changes;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Concepts.ReadModels;
using Cratis.Chronicle.Concepts.Sinks;
using Cratis.Chronicle.Dynamic;
using Cratis.Chronicle.Json;
using Cratis.Chronicle.Properties;
using Cratis.Chronicle.Storage.ReadModels;
using Cratis.Chronicle.Storage.Sinks;
using Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.ReadModels;
using Cratis.Monads;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage;

#pragma warning disable SA1204 // Mix of public instance + private helpers; suppressing avoids reshuffling the public API surface to satisfy the analyzer.

namespace Cratis.Chronicle.Storage.Sql.Sinks;

/// <summary>
/// Represents an implementation of <see cref="ISink"/> for working with projections in SQL.
/// </summary>
/// <param name="eventStoreName">The <see cref="Concepts.EventStoreName"/> the sink is for.</param>
/// <param name="namespace">The <see cref="Concepts.EventStoreNamespaceName"/> the sink is for.</param>
/// <param name="readModel">The <see cref="ReadModelDefinition"/> the sink is for.</param>
/// <param name="database">The <see cref="IDatabase"/> for accessing SQL storage.</param>
/// <param name="expandoObjectConverter">The <see cref="IExpandoObjectConverter"/> for converting between documents and <see cref="ExpandoObject"/>.</param>
public class Sink(
    Concepts.EventStoreName eventStoreName,
    Concepts.EventStoreNamespaceName @namespace,
    ReadModelDefinition readModel,
    IDatabase database,
    IExpandoObjectConverter expandoObjectConverter) : ISink
{
    static readonly IEnumerable<FailedPartition> _noFailedPartitions = [];

    readonly string _tableName = readModel.ContainerName.Value;

    /// <inheritdoc/>
    public SinkTypeName Name => "SQL";

    /// <inheritdoc/>
    public SinkTypeId TypeId => WellKnownSinkTypes.SQL;

    /// <inheritdoc/>
    public async Task<ExpandoObject?> FindOrDefault(Key key)
    {
        var id = GetIdString(key);
        await using var scope = await database.ReadModelTable(eventStoreName, @namespace, _tableName);
        var entry = await scope.DbContext.Entries.FirstOrDefaultAsync(e => e.Id == id);
        if (entry is null)
        {
            return null;
        }

        return DeserializeDocument(entry.Document);
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<FailedPartition>> ApplyChanges(
        Key key,
        IChangeset<AppendedEvent, ExpandoObject> changeset,
        EventSequenceNumber eventSequenceNumber)
    {
        var id = GetIdString(key);
        await using var scope = await database.ReadModelTable(eventStoreName, @namespace, _tableName);

        if (changeset.HasBeenRemoved())
        {
            var toRemove = await scope.DbContext.Entries.FirstOrDefaultAsync(e => e.Id == id);
            if (toRemove is not null)
            {
                scope.DbContext.Entries.Remove(toRemove);
                await scope.DbContext.SaveChangesAsync();
            }

            return _noFailedPartitions;
        }

        // Apply Joined changes (from `.Join<TEvent>` projection definitions) to every existing
        // read model whose join target matches — never upsert. Join semantics say "for any
        // already-projected document matching this property, also apply these changes"; an
        // upsert would manufacture a phantom document keyed on the join value (e.g. a User
        // keyed on a GroupId) and route the join's Set() calls to that document instead of
        // the real targets. MongoDB's sink expresses the same intent with UpdateManyAsync +
        // IsUpsert=false; this iterates the entries in C# because the document JSON is opaque
        // to EF's SQL translator.
        foreach (var joined in changeset.Changes.OfType<Joined>())
        {
            await ApplyJoinedChange(scope, joined, eventSequenceNumber);
        }

        // RemovedWithJoin yields ChildRemovedFromAll: pull a child with this identifier from
        // every parent document that contains it. MongoDB does this with UpdateMany + $pull.
        foreach (var childRemovedFromAll in changeset.Changes.OfType<ChildRemovedFromAll>())
        {
            await RemoveChildFromAllEntries(scope, childRemovedFromAll, eventSequenceNumber);
        }

        var nonJoinedChanges = changeset.Changes
            .Where(c => c is not Joined and not ChildRemovedFromAll)
            .ToArray();
        if (nonJoinedChanges.Length == 0)
        {
            return _noFailedPartitions;
        }

        var state = changeset.InitialState.Clone();
        state = ApplyActualChanges(key, nonJoinedChanges, state);

        // Ensure the document carries an identifier field, but match the case the projection
        // already uses (or the schema declares). Blindly writing a lowercase `id` causes
        // ExpandoObjectConverter to throw "Sequence contains more than one matching element"
        // when the schema declares `Id` (PascalCase) and the projection populated it — the
        // case-insensitive lookup finds both keys.
        var stateDict = (IDictionary<string, object?>)state;
        var hasIdProperty = stateDict.Keys.Any(k => string.Equals(k, "Id", StringComparison.OrdinalIgnoreCase));
        if (!hasIdProperty)
        {
            stateDict[GetIdentifierPropertyName()] = key.Value;
        }

        // __lastHandledEventSequenceNumber must only ever move forward. When events for the
        // same read-model key are processed out of order — as can happen when catch-up
        // dispatches per-partition steps for a constant-key or join projection — keeping the
        // initial state's value avoids letting an earlier sequence number overwrite a later
        // one, which would leave the read model pointing at a sequence it has already passed.
        var newSequenceNumber = (ulong)eventSequenceNumber;
        var existingSequenceNumber = stateDict.TryGetValue(WellKnownProperties.LasHandledEventSequenceNumber, out var existingValue) && existingValue is not null
            ? Convert.ToUInt64(existingValue)
            : 0UL;
        stateDict[WellKnownProperties.LasHandledEventSequenceNumber] = Math.Max(newSequenceNumber, existingSequenceNumber);

        var document = SerializeDocument(state);

        await UpsertEntry(scope, id, document);
        return _noFailedPartitions;
    }

    /// <inheritdoc/>
    public Task BeginBulk() => Task.CompletedTask;

    /// <inheritdoc/>
    public Task EndBulk() => Task.CompletedTask;

    /// <inheritdoc/>
    public Task BeginReplay(ReplayContext context) => Task.CompletedTask;

    /// <inheritdoc/>
    public Task ResumeReplay(ReplayContext context) => Task.CompletedTask;

    /// <inheritdoc/>
    public Task EndReplay(ReplayContext context) => Task.CompletedTask;

    /// <inheritdoc/>
    public async Task Remove(ReadModelContainerName containerName)
    {
        await using var scope = await database.Namespace(eventStoreName, @namespace);
        var sqlGenerationHelper = scope.DbContext.GetService<ISqlGenerationHelper>();
        var delimitedTableName = sqlGenerationHelper.DelimitIdentifier(containerName.Value);
        var sql = $"DROP TABLE IF EXISTS {delimitedTableName}";
        await scope.DbContext.Database.ExecuteSqlRawAsync(sql);
    }

    /// <inheritdoc/>
    public async Task PrepareInitialRun()
    {
        await using var scope = await database.ReadModelTable(eventStoreName, @namespace, _tableName);
        scope.DbContext.Entries.RemoveRange(scope.DbContext.Entries);
        await scope.DbContext.SaveChangesAsync();
    }

    /// <inheritdoc/>
    public Task EnsureIndexes() => Task.CompletedTask;

    /// <inheritdoc/>
    public async Task<Option<Key>> TryFindRootKeyByChildValue(PropertyPath childPropertyPath, object childValue)
    {
        if (childValue is null)
        {
            return Option<Key>.None();
        }

        var pathSegments = childPropertyPath.Segments.ToArray();

        await using var scope = await database.ReadModelTable(eventStoreName, @namespace, _tableName);
        var entries = await scope.DbContext.Entries.ToListAsync();

        foreach (var entry in entries)
        {
            var document = DeserializeDocument(entry.Document);
            if (TryFindValueInDocument(document, pathSegments, 0, childValue))
            {
                return new Option<Key>(new Key(entry.Id, ArrayIndexers.NoIndexers));
            }
        }

        return Option<Key>.None();
    }

    /// <inheritdoc/>
    public async Task<ReadModelInstances> GetInstances(ReadModelContainerName? occurrence = null, int skip = 0, int take = 50)
    {
        var containerName = occurrence?.Value ?? _tableName;
        await using var scope = await database.ReadModelTable(eventStoreName, @namespace, containerName);
        var totalCount = await scope.DbContext.Entries.CountAsync();
        var entries = await scope.DbContext.Entries
            .Skip(skip)
            .Take(take)
            .ToListAsync();

        var instances = entries
            .Select(e => DeserializeDocument(e.Document));

        return new ReadModelInstances(instances, totalCount);
    }

    async Task ApplyJoinedChange(DbContextScope<ReadModelDbContext> scope, Joined joined, EventSequenceNumber eventSequenceNumber)
    {
        // For a root-level join we match all entries whose `OnProperty` equals the join key —
        // e.g. all Users whose `GroupId` field equals the GroupId emitted by the join's event.
        // For a child-level join (key.ArrayIndexers populated) the join lives inside a parent
        // document's array; the array indexer's identifier carries the value to match against,
        // so we use it instead of the joined.Key (which holds the parent's root key, not the
        // child's identifier).
        var (onPropertySegments, joinValue) = !joined.ArrayIndexers.IsEmpty
            ? GetChildJoinFilter(joined)
            : (joined.OnProperty.Segments.ToArray(), joined.Key);

        if (joinValue is null)
        {
            return;
        }

        var entries = await scope.DbContext.Entries.ToListAsync();
        var modified = false;

        foreach (var entry in entries)
        {
            var document = DeserializeDocument(entry.Document);
            if (!TryFindValueInDocument(document, onPropertySegments, 0, joinValue))
            {
                continue;
            }

            var entryKey = new Key(entry.Id, ArrayIndexers.NoIndexers);
            var updated = ApplyActualChanges(entryKey, joined.Changes, document);
            var entryDict = (IDictionary<string, object?>)updated;

            // Move __lastHandledEventSequenceNumber forward on the joined target too — the sink
            // tracks per-document progress and the joined update extended the document's state.
            var newSequenceNumber = (ulong)eventSequenceNumber;
            var existingSequenceNumber = entryDict.TryGetValue(WellKnownProperties.LasHandledEventSequenceNumber, out var existingValue) && existingValue is not null
                ? Convert.ToUInt64(existingValue)
                : 0UL;
            entryDict[WellKnownProperties.LasHandledEventSequenceNumber] = Math.Max(newSequenceNumber, existingSequenceNumber);

            entry.Document = SerializeDocument(updated);
            scope.DbContext.Entries.Update(entry);
            modified = true;
        }

        if (modified)
        {
            await scope.DbContext.SaveChangesAsync();
        }
    }

    async Task RemoveChildFromAllEntries(DbContextScope<ReadModelDbContext> scope, ChildRemovedFromAll childRemoved, EventSequenceNumber eventSequenceNumber)
    {
        // Pull the matching child out of every entry that carries one. The childRemoved
        // describes the children-collection path and the identifier property on each child;
        // we deserialize each entry, drop matching items, and write a fresh List back to the
        // children property. Re-using the deserialized collection in place would NRE on a
        // child that came in as a JSON array (System.Text.Json materializes those as a fixed-
        // size object[], which throws on Remove); going through the property setter always
        // produces a mutable List so subsequent runs can keep editing it.
        var entries = await scope.DbContext.Entries.ToListAsync();
        var modified = false;
        var identifierSegment = childRemoved.IdentifiedByProperty.LastSegment.Value;
        var keyAsString = childRemoved.Key?.ToString();

        foreach (var entry in entries)
        {
            var document = DeserializeDocument(entry.Document);
            var collection = document.EnsureCollection<ExpandoObject, object>(childRemoved.ChildrenProperty, childRemoved.ArrayIndexers);
            var matched = false;
            var retained = new List<object>(collection.Count);
            foreach (var item in collection)
            {
                if (item is IDictionary<string, object?> itemDict
                    && itemDict.TryGetValue(identifierSegment, out var value)
                    && (Equals(value, childRemoved.Key)
                        || (value is not null && value.ToString() == keyAsString)))
                {
                    matched = true;
                    continue;
                }

                retained.Add(item);
            }

            if (!matched)
            {
                continue;
            }

            var childrenProperty = childRemoved.ChildrenProperty.LastSegment.Value;
            ((IDictionary<string, object?>)document)[childrenProperty] = retained;

            var docDict = (IDictionary<string, object?>)document;
            var newSequenceNumber = (ulong)eventSequenceNumber;
            var existingSequenceNumber = docDict.TryGetValue(WellKnownProperties.LasHandledEventSequenceNumber, out var existingValue) && existingValue is not null
                ? Convert.ToUInt64(existingValue)
                : 0UL;
            docDict[WellKnownProperties.LasHandledEventSequenceNumber] = Math.Max(newSequenceNumber, existingSequenceNumber);

            entry.Document = SerializeDocument(document);
            scope.DbContext.Entries.Update(entry);
            modified = true;
        }

        if (modified)
        {
            await scope.DbContext.SaveChangesAsync();
        }
    }

    static (IPropertyPathSegment[] Segments, object? Value) GetChildJoinFilter(Joined joined)
    {
        var lastIndexer = joined.ArrayIndexers.All.Last();
        var combined = lastIndexer.ArrayProperty + lastIndexer.IdentifierProperty;
        return (combined.Segments.ToArray(), lastIndexer.Identifier);
    }

    static async Task UpsertEntry(DbContextScope<ReadModelDbContext> scope, string id, string document)
    {
        // The Find-then-Add-or-Update path is a check-then-act race: when a projection and a
        // reducer write to the same read-model key concurrently (different observer grains,
        // both holding their own DbContext scope), both can see Find return null and both can
        // Add. The second SaveChanges then hits "UNIQUE constraint failed" / primary-key
        // violation, the partition fails, and any spec that waits on that partition times out.
        //
        // The fix is to absorb the duplicate-key error and reattempt as an Update. The retry
        // uses a fresh DbContext-attached entity so the second writer's content wins — which is
        // the desired last-writer-wins semantic for projection / reducer sinks.
        var existing = await scope.DbContext.Entries.FirstOrDefaultAsync(e => e.Id == id);
        if (existing is not null)
        {
            existing.Document = document;
            scope.DbContext.Entries.Update(existing);
            await scope.DbContext.SaveChangesAsync();
            return;
        }

        scope.DbContext.Entries.Add(new ReadModelEntry { Id = id, Document = document });
        try
        {
            await scope.DbContext.SaveChangesAsync();
        }
        catch (DbUpdateException)
        {
            // Another writer inserted the row between our Find and our SaveChanges. Detach the
            // failed-add tracker so the subsequent Update doesn't see two tracked entities for
            // the same key, then re-fetch and overwrite. If the row really doesn't exist (i.e.
            // the exception was something else entirely), re-throw so the caller sees it.
            var failedEntry = scope.DbContext.ChangeTracker.Entries<ReadModelEntry>().FirstOrDefault(e => e.Entity.Id == id);
            failedEntry?.State = EntityState.Detached;

            var winning = await scope.DbContext.Entries.FirstOrDefaultAsync(e => e.Id == id);
            if (winning is null)
            {
                throw;
            }

            winning.Document = document;
            scope.DbContext.Entries.Update(winning);
            await scope.DbContext.SaveChangesAsync();
        }
    }

    static string GetIdString(Key key)
    {
        if (key.Value is ExpandoObject expandoKey)
        {
            var parts = expandoKey
                .OrderBy(kv => kv.Key)
                .Select(kv => kv.Value?.ToString() ?? string.Empty);
            return string.Join('_', parts);
        }

        return key.Value?.ToString() ?? string.Empty;
    }

    static ExpandoObject ApplyActualChanges(Key key, IEnumerable<Change> changes, ExpandoObject state)
    {
        foreach (var change in changes)
        {
            switch (change)
            {
                case PropertiesChanged<ExpandoObject> propertiesChanged:
                    state = ApplyPropertiesChanged(state, propertiesChanged, key.ArrayIndexers);
                    break;

                case ChildAdded childAdded:
                    var collection = state.EnsureCollection<ExpandoObject, object>(childAdded.ChildrenProperty, key.ArrayIndexers);
                    collection.Add(childAdded.State);
                    break;

                case ChildRemoved childRemoved:
                    RemoveChild(state, childRemoved, key.ArrayIndexers);
                    break;

                case NestedCleared nestedCleared:
                    nestedCleared.NestedProperty.SetValue(state, null!, nestedCleared.ArrayIndexers);
                    break;

                case Joined joined:
                    state = ApplyActualChanges(key, joined.Changes, state);
                    break;

                case ResolvedJoin resolvedJoin:
                    state = ApplyActualChanges(key, resolvedJoin.Changes, state);
                    break;
            }
        }

        return state;
    }

    static void RemoveChild(ExpandoObject state, ChildRemoved childRemoved, ArrayIndexers parentArrayIndexers)
    {
        // ChildRemoved excludes its own ChildrenProperty from the indexers used to navigate the
        // parent document — we only need indexers for any ENCLOSING arrays (e.g. parent-of-parent).
        // Write a fresh List back to the children property: a children array deserialized from
        // JSON is a fixed-size object[], so calling Remove on it directly throws
        // NotSupportedException and the partition stalls.
        var arrayIndexers = new ArrayIndexers(parentArrayIndexers.All.Where(_ => !_.ArrayProperty.Equals(childRemoved.ChildrenProperty)));
        var collection = state.EnsureCollection<ExpandoObject, object>(childRemoved.ChildrenProperty, arrayIndexers);
        var identifierSegment = childRemoved.IdentifiedByProperty.LastSegment.Value;
        var keyAsString = childRemoved.Key?.ToString();
        var retained = new List<object>(collection.Count);
        foreach (var item in collection)
        {
            if (item is IDictionary<string, object?> itemDict
                && itemDict.TryGetValue(identifierSegment, out var value)
                && (Equals(value, childRemoved.Key)
                    || (value is not null && value.ToString() == keyAsString)))
            {
                continue;
            }

            retained.Add(item);
        }

        var childrenProperty = childRemoved.ChildrenProperty.LastSegment.Value;
        ((IDictionary<string, object?>)state)[childrenProperty] = retained;
    }

    static ExpandoObject ApplyPropertiesChanged(ExpandoObject state, PropertiesChanged<ExpandoObject> propertiesChanged, ArrayIndexers keyArrayIndexers)
    {
        // The Changeset emits PropertiesChanged with State populated (projections) or with
        // State left null and Differences populated (reducers). Treat Differences as the
        // canonical source so the sink does not depend on which producer built the change —
        // applying each diff at its PropertyPath produces an equivalent merged document
        // and matches the contract that MongoDB sinks already follow.
        foreach (var difference in propertiesChanged.Differences)
        {
            var arrayIndexers = difference.ArrayIndexers ?? keyArrayIndexers;
            difference.PropertyPath.SetValue(state, difference.Changed!, arrayIndexers);
        }

        return state;
    }

    static bool ValuesAreEqual(object value, object targetValue)
    {
        if (value.Equals(targetValue))
        {
            return true;
        }

        return value.ToString() == targetValue.ToString();
    }

    string SerializeDocument(ExpandoObject state)
    {
        var schema = readModel.GetSchemaForLatestGeneration();
        var jsonObject = expandoObjectConverter.ToJsonObject(state, schema);

        // __lastHandledEventSequenceNumber is a system property not present in the user-defined schema.
        // Preserve it explicitly so it survives the JSON round-trip.
        var stateDict = (IDictionary<string, object?>)state;
        if (stateDict.TryGetValue(WellKnownProperties.LasHandledEventSequenceNumber, out var seqObj) && seqObj is not null)
        {
            try
            {
                jsonObject[WellKnownProperties.LasHandledEventSequenceNumber] = JsonValue.Create(Convert.ToUInt64(seqObj));
            }
            catch
            {
                // Ignore conversion errors — the property will be absent from the stored document.
            }
        }

        return jsonObject.ToJsonString();
    }

    string GetIdentifierPropertyName()
    {
        var schema = readModel.GetSchemaForLatestGeneration();
        if (schema.Properties.ContainsKey("Id"))
        {
            return "Id";
        }

        return "id";
    }

    ExpandoObject DeserializeDocument(string document)
    {
        var schema = readModel.GetSchemaForLatestGeneration();
        var jsonObject = JsonNode.Parse(document) as JsonObject ?? new JsonObject();
        var result = expandoObjectConverter.ToExpandoObject(jsonObject, schema);

        // __lastHandledEventSequenceNumber is not in the user schema so ToExpandoObject drops it.
        // Restore it from the raw JSON so callers can read it back as a ulong.
        if (jsonObject.TryGetPropertyValue(WellKnownProperties.LasHandledEventSequenceNumber, out var seqNode) && seqNode is not null)
        {
            try
            {
                var resultDict = (IDictionary<string, object?>)result;
                resultDict[WellKnownProperties.LasHandledEventSequenceNumber] = seqNode.GetValue<ulong>();
            }
            catch
            {
                // Ignore — leave the property absent rather than crashing.
            }
        }

        return result;
    }

    bool TryFindValueInDocument(ExpandoObject document, IPropertyPathSegment[] pathSegments, int segmentIndex, object targetValue)
    {
        if (segmentIndex >= pathSegments.Length)
        {
            return false;
        }

        var currentSegment = pathSegments[segmentIndex];
        var dict = (IDictionary<string, object?>)document;

        if (!dict.TryGetValue(currentSegment.Value, out var value) || value is null)
        {
            return false;
        }

        if (segmentIndex == pathSegments.Length - 1)
        {
            return ValuesAreEqual(value, targetValue);
        }

        if (value is IEnumerable<object> collection)
        {
            foreach (var item in collection.OfType<ExpandoObject>())
            {
                if (TryFindValueInDocument(item, pathSegments, segmentIndex + 1, targetValue))
                {
                    return true;
                }
            }
        }
        else if (value is ExpandoObject nestedExpando)
        {
            return TryFindValueInDocument(nestedExpando, pathSegments, segmentIndex + 1, targetValue);
        }

        return false;
    }
}
