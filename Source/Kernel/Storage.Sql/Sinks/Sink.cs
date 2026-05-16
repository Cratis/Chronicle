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

        var state = changeset.InitialState.Clone();
        state = ApplyActualChanges(key, changeset.Changes, state);
        ((dynamic)state).id = key.Value;
        ((IDictionary<string, object?>)state)[WellKnownProperties.LasHandledEventSequenceNumber] = (ulong)eventSequenceNumber;

        var document = SerializeDocument(state);

        var existing = await scope.DbContext.Entries.FirstOrDefaultAsync(e => e.Id == id);
        if (existing is null)
        {
            scope.DbContext.Entries.Add(new ReadModelEntry { Id = id, Document = document });
        }
        else
        {
            existing.Document = document;
            scope.DbContext.Entries.Update(existing);
        }

        await scope.DbContext.SaveChangesAsync();
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
                case PropertiesChanged<ExpandoObject>:
                    state = state.MergeWith((change.State as ExpandoObject)!);
                    break;

                case ChildAdded childAdded:
                    var collection = state.EnsureCollection<ExpandoObject, object>(childAdded.ChildrenProperty, key.ArrayIndexers);
                    collection.Add(childAdded.State);
                    break;

                case NestedCleared nestedCleared:
                    var stateDict = (IDictionary<string, object?>)state;
                    stateDict[nestedCleared.NestedProperty.LastSegment.Value] = null;
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
        return jsonObject.ToJsonString();
    }

    ExpandoObject DeserializeDocument(string document)
    {
        var schema = readModel.GetSchemaForLatestGeneration();
        var jsonObject = JsonNode.Parse(document) as JsonObject ?? new JsonObject();
        return expandoObjectConverter.ToExpandoObject(jsonObject, schema);
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
