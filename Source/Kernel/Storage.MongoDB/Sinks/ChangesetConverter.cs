// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Chronicle.Changes;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Concepts.ReadModels;
using Cratis.Chronicle.Properties;
using Cratis.Chronicle.Schemas;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Cratis.Chronicle.Storage.MongoDB.Sinks;

/// <summary>
/// Represents an implementation of <see cref="IChangesetConverter"/>.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="ChangesetConverter"/> class.
/// </remarks>
/// <param name="readModel">The <see cref="ReadModelDefinition"/> the sink is for.</param>
/// <param name="converter"><see cref="IMongoDBConverter"/> to use.</param>
/// <param name="collections"><see cref="ISinkCollections"/> to use.</param>
/// <param name="expandoObjectConverter"><see cref="IExpandoObjectConverter"/> for converting between documents and <see cref="ExpandoObject"/>.</param>
public class ChangesetConverter(
    ReadModelDefinition readModel,
    IMongoDBConverter converter,
    ISinkCollections collections,
    IExpandoObjectConverter expandoObjectConverter) : IChangesetConverter
{
    /// <inheritdoc/>
    public async Task<UpdateDefinitionAndArrayFilters> ToUpdateDefinition(
        Key key,
        IChangeset<AppendedEvent, ExpandoObject> changeset,
        EventSequenceNumber eventSequenceNumber)
    {
        var hasChanges = false;
        var updateDefinitionBuilder = Builders<BsonDocument>.Update;
        UpdateDefinition<BsonDocument>? updateBuilder = default;
        var normalizedChanges = NormalizeJoinedChanges(changeset.Changes);

        var arrayFiltersForDocument = new ArrayFilters();
        await ApplyActualChanges(
            key,
            normalizedChanges,
            updateDefinitionBuilder,
            ref updateBuilder,
            ref hasChanges,
            arrayFiltersForDocument,
            eventSequenceNumber);

        if (hasChanges)
        {
            BuildLastHandledEventSequenceNumber(updateDefinitionBuilder, ref updateBuilder, eventSequenceNumber);
        }

        var distinctArrayFilters = arrayFiltersForDocument.DistinctBy(_ => _.Document).ToArray();

        return new(updateBuilder!, distinctArrayFilters, hasChanges);
    }

    static bool TryMergeJoinedChangeIntoChildAdded(IList<Change> changes, Joined joined)
    {
        if (joined.ArrayIndexers.IsEmpty)
        {
            return false;
        }

        var targetIndexer = joined.ArrayIndexers.All.Last();
        var matchingChild = changes
            .OfType<ChildAdded>()
            .FirstOrDefault(_ =>
                _.ChildrenProperty == targetIndexer.ArrayProperty &&
                _.IdentifiedByProperty == targetIndexer.IdentifierProperty &&
                _.Key.ToString() == joined.Key.ToString());

        if (matchingChild?.Child is not ExpandoObject child)
        {
            return false;
        }

        ApplyJoinedChangesToChild(child, joined.Changes);
        return true;
    }

    static void ApplyJoinedChangesToChild(ExpandoObject child, IEnumerable<Change> changes)
    {
        foreach (var change in changes)
        {
            switch (change)
            {
                case PropertiesChanged<ExpandoObject> propertiesChanged:
                    foreach (var difference in propertiesChanged.Differences.Where(_ => !_.PropertyPath.IsMongoDBKey()))
                    {
                        difference.PropertyPath.SetValue(child, difference.Changed!, ArrayIndexers.NoIndexers);
                    }

                    break;

                case ResolvedJoin resolvedJoin:
                    ApplyJoinedChangesToChild(child, resolvedJoin.Changes);
                    break;
            }
        }
    }

    Task ApplyActualChanges(
        Key key,
        IEnumerable<Change> changes,
        UpdateDefinitionBuilder<BsonDocument> updateDefinitionBuilder,
        ref UpdateDefinition<BsonDocument>? updateBuilder,
        ref bool hasChanges,
        ArrayFilters arrayFiltersForDocument,
        EventSequenceNumber eventSequenceNumber)
    {
        var joinTasks = new List<Task>();

        foreach (var change in changes)
        {
            switch (change)
            {
                case PropertiesChanged<ExpandoObject> propertiesChanged:
                    hasChanges |= BuildPropertiesChanged(updateDefinitionBuilder, ref updateBuilder, arrayFiltersForDocument, propertiesChanged);
                    break;

                case ChildAdded childAdded:
                    BuildChildAdded(key, updateDefinitionBuilder, ref updateBuilder, arrayFiltersForDocument, childAdded);
                    hasChanges = true;
                    break;

                case ChildRemoved childRemoved:
                    BuildChildRemoved(key, updateDefinitionBuilder, ref updateBuilder, arrayFiltersForDocument, childRemoved);
                    hasChanges = true;
                    break;

                case NestedCleared nestedCleared:
                    BuildNestedCleared(updateDefinitionBuilder, ref updateBuilder, arrayFiltersForDocument, nestedCleared);
                    hasChanges = true;
                    break;

                case Joined joined:
                    joinTasks.Add(PerformJoined(key, updateDefinitionBuilder, joined, eventSequenceNumber));
                    break;

                case ResolvedJoin resolvedJoined:
                    var applyActualChangesTask = ApplyActualChanges(key, resolvedJoined.Changes, updateDefinitionBuilder, ref updateBuilder, ref hasChanges, arrayFiltersForDocument, eventSequenceNumber);
                    joinTasks.Add(applyActualChangesTask);
                    break;
            }
        }

        return Task.WhenAll(joinTasks);
    }

    void BuildLastHandledEventSequenceNumber(UpdateDefinitionBuilder<BsonDocument> updateDefinitionBuilder, ref UpdateDefinition<BsonDocument>? updateBuilder, EventSequenceNumber eventSequenceNumber)
    {
        var value = converter.ToBsonValue(eventSequenceNumber);

        if (updateBuilder != default)
        {
            updateBuilder = updateBuilder.Set(WellKnownProperties.LasHandledEventSequenceNumber, value);
        }
        else
        {
            updateBuilder = updateDefinitionBuilder.Set(WellKnownProperties.LasHandledEventSequenceNumber, value);
        }
    }

    bool BuildPropertiesChanged(UpdateDefinitionBuilder<BsonDocument> updateDefinitionBuilder, ref UpdateDefinition<BsonDocument>? updateBuilder, ArrayFilters arrayFiltersForDocument, PropertiesChanged<ExpandoObject> propertiesChanged)
    {
        var allArrayFilters = new List<BsonDocumentArrayFilterDefinition<BsonDocument>>();

        foreach (var propertyDifference in propertiesChanged.Differences.Where(_ => !_.PropertyPath.IsMongoDBKey()).ToArray())
        {
            var (property, arrayFilters) = converter.ToMongoDBProperty(propertyDifference.PropertyPath, propertyDifference.ArrayIndexers);
            allArrayFilters.AddRange(arrayFilters);

            var value = converter.ToBsonValue(propertyDifference.Changed, propertyDifference.PropertyPath);

            if (updateBuilder != default)
            {
                updateBuilder = updateBuilder.Set(property, value);
            }
            else
            {
                updateBuilder = updateDefinitionBuilder.Set(property, value);
            }
        }

        arrayFiltersForDocument.AddRange(allArrayFilters);
        return propertiesChanged.Differences.Any();
    }

    void BuildChildAdded(Key key, UpdateDefinitionBuilder<BsonDocument> updateDefinitionBuilder, ref UpdateDefinition<BsonDocument>? updateBuilder, ArrayFilters arrayFiltersForDocument, ChildAdded childAdded)
    {
        BsonValue bsonValue;

        var stateType = childAdded.State.GetType();
        if (stateType.IsPrimitive || stateType == typeof(string) || stateType == typeof(Guid) || stateType == typeof(DateTime) || stateType == typeof(DateTimeOffset) || stateType == typeof(DateOnly) || stateType == typeof(TimeOnly))
        {
            bsonValue = childAdded.State.ToBsonValue();
        }
        else
        {
            var schema = readModel.GetSchemaForLatestGeneration().GetSchemaForPropertyPath(childAdded.ChildrenProperty);
            bsonValue = expandoObjectConverter.ToBsonDocument((childAdded.State as ExpandoObject)!, schema);
        }

        var childrenProperty = childAdded.ChildrenProperty.GetChildrenProperty();
        var arrayIndexers = new ArrayIndexers(key.ArrayIndexers.All.Where(_ => !_.ArrayProperty.Equals(childAdded.ChildrenProperty)));
        var (property, arrayFilters) = converter.ToMongoDBProperty(childrenProperty, arrayIndexers);
        arrayFiltersForDocument.AddRange(arrayFilters);

        updateBuilder = updateBuilder is not null
            ? updateBuilder.Push(property, bsonValue)
            : updateDefinitionBuilder.Push(property, bsonValue);
    }

    void BuildNestedCleared(UpdateDefinitionBuilder<BsonDocument> updateDefinitionBuilder, ref UpdateDefinition<BsonDocument>? updateBuilder, ArrayFilters arrayFiltersForDocument, NestedCleared nestedCleared)
    {
        var (property, arrayFilters) = converter.ToMongoDBProperty(nestedCleared.NestedProperty, nestedCleared.ArrayIndexers);
        arrayFiltersForDocument.AddRange(arrayFilters);

        updateBuilder = updateBuilder is not null
            ? updateBuilder.Set(property, BsonNull.Value)
            : updateDefinitionBuilder.Set(property, BsonNull.Value);
    }

    void BuildChildRemoved(Key key, UpdateDefinitionBuilder<BsonDocument> updateDefinitionBuilder, ref UpdateDefinition<BsonDocument>? updateBuilder, ArrayFilters arrayFiltersForDocument, ChildRemoved childRemoved)
    {
        BsonValue bsonValue;

        var stateType = childRemoved.State.GetType();
        if (stateType.IsPrimitive || stateType == typeof(string) || stateType == typeof(Guid) || stateType == typeof(DateTime) || stateType == typeof(DateTimeOffset) || stateType == typeof(DateOnly) || stateType == typeof(TimeOnly))
        {
            bsonValue = childRemoved.State.ToBsonValue();
        }
        else
        {
            var schema = readModel.GetSchemaForLatestGeneration().GetSchemaForPropertyPath(childRemoved.ChildrenProperty);
            bsonValue = expandoObjectConverter.ToBsonDocument((childRemoved.State as ExpandoObject)!, schema);
        }

        var childrenProperty = childRemoved.ChildrenProperty.GetChildrenProperty();
        var arrayIndexers = new ArrayIndexers(key.ArrayIndexers.All.Where(_ => !_.ArrayProperty.Equals(childRemoved.ChildrenProperty)));
        var (property, arrayFilters) = converter.ToMongoDBProperty(childrenProperty, arrayIndexers);
        arrayFiltersForDocument.AddRange(arrayFilters);

        updateBuilder = updateBuilder is not null
            ? updateBuilder.Pull(property, bsonValue)
            : updateDefinitionBuilder.Pull(property, bsonValue);
    }

    async Task PerformJoined(Key key, UpdateDefinitionBuilder<BsonDocument> updateDefinitionBuilder, Joined joined, EventSequenceNumber eventSequenceNumber)
    {
        UpdateDefinition<BsonDocument>? joinUpdateBuilder = default;
        var hasJoinChanges = false;
        var collection = collections.GetCollection();

        var joinArrayFiltersForDocument = new ArrayFilters();
        await ApplyActualChanges(key, joined.Changes, updateDefinitionBuilder, ref joinUpdateBuilder, ref hasJoinChanges, joinArrayFiltersForDocument, eventSequenceNumber);

        if (!hasJoinChanges)
        {
            return;
        }
        BuildLastHandledEventSequenceNumber(updateDefinitionBuilder, ref joinUpdateBuilder, eventSequenceNumber);
        var filter = CreateJoinedFilterDefinition(key, joined);
        await collection.UpdateManyAsync(
            filter,
            joinUpdateBuilder,
            new UpdateOptions
            {
                IsUpsert = false,
                ArrayFilters = [.. joinArrayFiltersForDocument]
            });
    }

    FilterDefinition<BsonDocument> CreateJoinedFilterDefinition(Key key, Joined joined)
    {
        // When array indexers are present, the join key (key.Value) is the root document key — not
        // the child identifier. Use the last array indexer's property path and value to build a filter
        // that matches ALL root documents containing this child, so UpdateManyAsync updates every
        // document that has the child (e.g. all groups that contain the user), not just the first one
        // found during key resolution.
        if (!key.ArrayIndexers.IsEmpty)
        {
            var lastIndexer = key.ArrayIndexers.All.Last();
            var filterPath = lastIndexer.ArrayProperty + lastIndexer.IdentifierProperty;
            var (property, _) = converter.ToMongoDBProperty(filterPath, ArrayIndexers.NoIndexers);
            var value = converter.ToBsonValue(lastIndexer.Identifier, filterPath);
            return Builders<BsonDocument>.Filter.Eq(property, value);
        }

        var (prop, _) = converter.ToMongoDBProperty(joined.OnProperty, ArrayIndexers.NoIndexers);
        return Builders<BsonDocument>.Filter.Eq(prop, joined.Key);
    }

    IEnumerable<Change> NormalizeJoinedChanges(IEnumerable<Change> changes)
    {
        var normalizedChanges = changes.ToList();
        var joinedChangesToSkip = new HashSet<Joined>();

        foreach (var joined in normalizedChanges.OfType<Joined>())
        {
            if (!TryMergeJoinedChangeIntoChildAdded(normalizedChanges, joined))
            {
                continue;
            }

            joinedChangesToSkip.Add(joined);
        }

        return normalizedChanges.Where(change => change is not Joined joined || !joinedChangesToSkip.Contains(joined));
    }
}
