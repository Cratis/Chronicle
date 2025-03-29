// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Chronicle.Changes;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Concepts.Models;
using Cratis.Chronicle.Properties;
using Cratis.Chronicle.Schemas;
using Cratis.Reflection;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Cratis.Chronicle.Storage.MongoDB.Sinks;

/// <summary>
/// Represents an implementation of <see cref="IChangesetConverter"/>.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="ChangesetConverter"/> class.
/// </remarks>
/// <param name="model">The <see cref="Model"/> the sink is for.</param>
/// <param name="converter"><see cref="IMongoDBConverter"/> to use.</param>
/// <param name="collections"><see cref="ISinkCollections"/> to use.</param>
/// <param name="expandoObjectConverter"><see cref="IExpandoObjectConverter"/> for converting between documents and <see cref="ExpandoObject"/>.</param>
public class ChangesetConverter(
    Model model,
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

        var arrayFiltersForDocument = new ArrayFilters();
        await ApplyActualChanges(
            key,
            changeset.Changes,
            updateDefinitionBuilder,
            ref updateBuilder,
            ref hasChanges,
            arrayFiltersForDocument,
            eventSequenceNumber);

        var distinctArrayFilters = arrayFiltersForDocument.DistinctBy(_ => _.Document).ToArray();

        return new(updateBuilder!, distinctArrayFilters, hasChanges);
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
                    hasChanges |= BuildPropertiesChanged(updateDefinitionBuilder, ref updateBuilder, arrayFiltersForDocument, propertiesChanged, eventSequenceNumber);
                    break;

                case ChildAdded childAdded:
                    BuildChildAdded(key, updateDefinitionBuilder, ref updateBuilder, arrayFiltersForDocument, childAdded, eventSequenceNumber);
                    hasChanges = true;
                    break;

                case ChildRemoved childRemoved:
                    BuildChildRemoved(key, updateDefinitionBuilder, ref updateBuilder, arrayFiltersForDocument, childRemoved, eventSequenceNumber);
                    hasChanges = true;
                    break;

                case Joined joined:
                    PerformJoined(key, updateDefinitionBuilder, joinTasks, joined, eventSequenceNumber);
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

    bool BuildPropertiesChanged(UpdateDefinitionBuilder<BsonDocument> updateDefinitionBuilder, ref UpdateDefinition<BsonDocument>? updateBuilder, ArrayFilters arrayFiltersForDocument, PropertiesChanged<ExpandoObject> propertiesChanged, EventSequenceNumber eventSequenceNumber)
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
        var hasChanges = propertiesChanged.Differences.Any();
        if (hasChanges)
        {
            BuildLastHandledEventSequenceNumber(updateDefinitionBuilder, ref updateBuilder, eventSequenceNumber);
        }

        return hasChanges;
    }

    void BuildChildAdded(Key key, UpdateDefinitionBuilder<BsonDocument> updateDefinitionBuilder, ref UpdateDefinition<BsonDocument>? updateBuilder, ArrayFilters arrayFiltersForDocument, ChildAdded childAdded, EventSequenceNumber eventSequenceNumber)
    {
        BsonValue bsonValue;

        if (childAdded.State.GetType().IsAPrimitiveType())
        {
            bsonValue = childAdded.State.ToBsonValue();
        }
        else
        {
            var schema = model.Schema.GetSchemaForPropertyPath(childAdded.ChildrenProperty);
            bsonValue = expandoObjectConverter.ToBsonDocument((childAdded.State as ExpandoObject)!, schema);
        }

        var childrenProperty = childAdded.ChildrenProperty.GetChildrenProperty();
        var arrayIndexers = new ArrayIndexers(key.ArrayIndexers.All.Where(_ => !_.ArrayProperty.Equals(childAdded.ChildrenProperty)));
        var (property, arrayFilters) = converter.ToMongoDBProperty(childrenProperty, arrayIndexers);
        arrayFiltersForDocument.AddRange(arrayFilters);

        updateBuilder = updateBuilder is not null
            ? updateBuilder.Push(property, bsonValue)
            : updateDefinitionBuilder.Push(property, bsonValue);

        BuildLastHandledEventSequenceNumber(updateDefinitionBuilder, ref updateBuilder, eventSequenceNumber);
    }

    void BuildChildRemoved(Key key, UpdateDefinitionBuilder<BsonDocument> updateDefinitionBuilder, ref UpdateDefinition<BsonDocument>? updateBuilder, ArrayFilters arrayFiltersForDocument, ChildRemoved childRemoved, EventSequenceNumber eventSequenceNumber)
    {
        BsonValue bsonValue;

        if (childRemoved.State.GetType().IsAPrimitiveType())
        {
            bsonValue = childRemoved.State.ToBsonValue();
        }
        else
        {
            var schema = model.Schema.GetSchemaForPropertyPath(childRemoved.ChildrenProperty);
            bsonValue = expandoObjectConverter.ToBsonDocument((childRemoved.State as ExpandoObject)!, schema);
        }

        var childrenProperty = childRemoved.ChildrenProperty.GetChildrenProperty();
        var arrayIndexers = new ArrayIndexers(key.ArrayIndexers.All.Where(_ => !_.ArrayProperty.Equals(childRemoved.ChildrenProperty)));
        var (property, arrayFilters) = converter.ToMongoDBProperty(childrenProperty, arrayIndexers);
        arrayFiltersForDocument.AddRange(arrayFilters);

        updateBuilder = updateBuilder is not null
            ? updateBuilder.Pull(property, bsonValue)
            : updateDefinitionBuilder.Pull(property, bsonValue);

        BuildLastHandledEventSequenceNumber(updateDefinitionBuilder, ref updateBuilder, eventSequenceNumber);
    }

    void PerformJoined(Key key, UpdateDefinitionBuilder<BsonDocument> updateDefinitionBuilder, List<Task> joinTasks, Joined joined, EventSequenceNumber eventSequenceNumber)
    {
        UpdateDefinition<BsonDocument>? joinUpdateBuilder = default;
        var hasJoinChanges = false;
        var collection = collections.GetCollection();

        var joinArrayFiltersForDocument = new ArrayFilters();
        ApplyActualChanges(key, joined.Changes, updateDefinitionBuilder, ref joinUpdateBuilder, ref hasJoinChanges, joinArrayFiltersForDocument, eventSequenceNumber).Wait();

        if (!hasJoinChanges)
        {
            return;
        }
        BuildLastHandledEventSequenceNumber(updateDefinitionBuilder, ref joinUpdateBuilder, eventSequenceNumber);
        var filter = CreateJoinedFilterDefinition(key, joined);
        joinTasks.Add(collection.UpdateManyAsync(
            filter,
            joinUpdateBuilder,
            new UpdateOptions
            {
                IsUpsert = false,
                ArrayFilters = [.. joinArrayFiltersForDocument]
            }));
    }

    FilterDefinition<BsonDocument> CreateJoinedFilterDefinition(Key key, Joined joined)
    {
        if (!key.ArrayIndexers.IsEmpty)
        {
            return FilterDefinition<BsonDocument>.Empty;
        }

        var (property, _) = converter.ToMongoDBProperty(joined.OnProperty, joined.ArrayIndexers);
        return Builders<BsonDocument>.Filter.Eq(property, key.Value);
    }
}
