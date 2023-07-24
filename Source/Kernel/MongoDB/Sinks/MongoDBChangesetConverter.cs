// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Aksio.Cratis.Changes;
using Aksio.Cratis.Events;
using Aksio.Cratis.Kernel.Engines.Projections;
using Aksio.Cratis.Projections;
using Aksio.Cratis.Properties;
using Aksio.Cratis.Schemas;
using MongoDB.Bson;
using MongoDB.Driver;
using NJsonSchema;

namespace Aksio.Cratis.Kernel.MongoDB.Sinks;

/// <summary>
/// Represents an implementation of <see cref="IMongoDBChangesetConverter"/>.
/// </summary>
public class MongoDBChangesetConverter : IMongoDBChangesetConverter
{
    readonly Model _model;
    readonly IMongoDBConverter _converter;
    readonly IExpandoObjectConverter _expandoObjectConverter;
    readonly IMongoDBSinkCollections _collections;

    /// <summary>
    /// Initializes a new instance of the <see cref="MongoDBChangesetConverter"/> class.
    /// </summary>
    /// <param name="model">The <see cref="Model"/> the sink is for.</param>
    /// <param name="converter"><see cref="IMongoDBConverter"/> to use.</param>
    /// <param name="collections"><see cref="IMongoDBSinkCollections"/> to use.</param>
    /// <param name="expandoObjectConverter"><see cref="IExpandoObjectConverter"/> for converting between documents and <see cref="ExpandoObject"/>.</param>
    public MongoDBChangesetConverter(
        Model model,
        IMongoDBConverter converter,
        IMongoDBSinkCollections collections,
        IExpandoObjectConverter expandoObjectConverter)
    {
        _model = model;
        _converter = converter;
        _collections = collections;
        _expandoObjectConverter = expandoObjectConverter;
    }

    /// <inheritdoc/>
    public async Task<MongoDBUpdateDefinitionAndArrayFilters> ToUpdateDefinition(Key key, IChangeset<AppendedEvent, ExpandoObject> changeset, bool isReplaying)
    {
        var hasChanges = false;
        var updateDefinitionBuilder = Builders<BsonDocument>.Update;
        UpdateDefinition<BsonDocument>? updateBuilder = default;

        var arrayFiltersForDocument = new List<BsonDocumentArrayFilterDefinition<BsonDocument>>();
        await ApplyActualChanges(key, changeset.Changes, updateDefinitionBuilder, ref updateBuilder, ref hasChanges, arrayFiltersForDocument, isReplaying);
        var distinctArrayFilters = arrayFiltersForDocument.DistinctBy(_ => _.Document).ToArray();

        return new(updateBuilder!, distinctArrayFilters, hasChanges);
    }

    Task ApplyActualChanges(
        Key key,
        IEnumerable<Change> changes,
        UpdateDefinitionBuilder<BsonDocument> updateDefinitionBuilder,
        ref UpdateDefinition<BsonDocument>? updateBuilder,
        ref bool hasChanges,
        List<BsonDocumentArrayFilterDefinition<BsonDocument>> arrayFiltersForDocument,
        bool isReplaying)
    {
        var joinTasks = new List<Task>();

        foreach (var change in changes)
        {
            switch (change)
            {
                case PropertiesChanged<ExpandoObject> propertiesChanged:
                    hasChanges = BuildPropertiesChanged(key, updateDefinitionBuilder, ref updateBuilder, arrayFiltersForDocument, propertiesChanged);
                    break;

                case ChildAdded childAdded:
                    BuildChildAdded(key, updateDefinitionBuilder, ref updateBuilder, arrayFiltersForDocument, childAdded);
                    hasChanges = true;
                    break;

                case Joined joined:
                    {
                        BuildJoined(key, updateDefinitionBuilder, isReplaying, joinTasks, joined);
                    }
                    break;

                case ResolvedJoin resolvedJoined:
                    {
                        ApplyActualChanges(key, resolvedJoined.Changes, updateDefinitionBuilder, ref updateBuilder, ref hasChanges, arrayFiltersForDocument, isReplaying);
                    }
                    break;
            }
        }

        return Task.WhenAll(joinTasks);
    }

    bool BuildPropertiesChanged(Key key, UpdateDefinitionBuilder<BsonDocument> updateDefinitionBuilder, ref UpdateDefinition<BsonDocument>? updateBuilder, List<BsonDocumentArrayFilterDefinition<BsonDocument>> arrayFiltersForDocument, PropertiesChanged<ExpandoObject> propertiesChanged)
    {
        var allArrayFilters = new List<BsonDocumentArrayFilterDefinition<BsonDocument>>();

        foreach (var propertyDifference in propertiesChanged.Differences)
        {
            var (property, arrayFilters) = _converter.ToMongoDBProperty(propertyDifference.PropertyPath, key.ArrayIndexers);
            allArrayFilters.AddRange(arrayFilters);

            var value = _converter.ToBsonValue(propertyDifference.Changed, propertyDifference.PropertyPath);

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

    void BuildChildAdded(Key key, UpdateDefinitionBuilder<BsonDocument> updateDefinitionBuilder, ref UpdateDefinition<BsonDocument>? updateBuilder, List<BsonDocumentArrayFilterDefinition<BsonDocument>> arrayFiltersForDocument, ChildAdded childAdded)
    {
        var schema = _model.Schema.GetSchemaForPropertyPath(childAdded.ChildrenProperty);
        var document = _expandoObjectConverter.ToBsonDocument((childAdded.State as ExpandoObject)!, schema);

        var segments = childAdded.ChildrenProperty.Segments.ToArray();
        var childrenProperty = new PropertyPath(string.Empty);
        for (var i = 0; i < segments.Length - 1; i++)
        {
            childrenProperty += segments[i].ToString()!;
        }

        childrenProperty += segments[^1].Value;
        var arrayIndexers = new ArrayIndexers(key.ArrayIndexers.All.Where(_ => !_.ArrayProperty.Equals(childAdded.ChildrenProperty)));
        var (property, arrayFilters) = _converter.ToMongoDBProperty(childrenProperty, arrayIndexers);
        arrayFiltersForDocument.AddRange(arrayFilters);

        if (updateBuilder is not null)
        {
            updateBuilder = updateBuilder.Push(property, document);
        }
        else
        {
            updateBuilder = updateDefinitionBuilder.Push(property, document);
        }
    }

void BuildJoined(Key key, UpdateDefinitionBuilder<BsonDocument> updateDefinitionBuilder, bool isReplaying, List<Task> joinTasks, Joined joined)
    {
        var (property, _) = _converter.ToMongoDBProperty(joined.OnProperty, joined.ArrayIndexers);

        UpdateDefinition<BsonDocument>? joinUpdateBuilder = default;
        var hasJoinChanges = false;

        var filter = Builders<BsonDocument>.Filter.Eq(property, joined.Key);

        var collection = _collections.GetCollection(isReplaying);

        var joinArrayFiltersForDocument = new List<BsonDocumentArrayFilterDefinition<BsonDocument>>();
        ApplyActualChanges(key, joined.Changes, updateDefinitionBuilder, ref joinUpdateBuilder, ref hasJoinChanges, joinArrayFiltersForDocument, isReplaying).Wait();

        if (hasJoinChanges)
        {
            joinTasks.Add(collection.UpdateOneAsync(
                filter,
                joinUpdateBuilder,
                new UpdateOptions
                {
                    IsUpsert = false,
                    ArrayFilters = joinArrayFiltersForDocument.ToArray()
                }));
        }
    }
}
