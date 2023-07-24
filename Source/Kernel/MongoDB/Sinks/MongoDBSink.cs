// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Aksio.Cratis.Changes;
using Aksio.Cratis.Events;
using Aksio.Cratis.Kernel.Engines.Projections;
using Aksio.Cratis.Kernel.Engines.Sinks;
using Aksio.Cratis.Projections;
using Aksio.Cratis.Properties;
using Aksio.Cratis.Schemas;
using Aksio.Cratis.Sinks;
using MongoDB.Bson;
using MongoDB.Driver;
using NJsonSchema;

namespace Aksio.Cratis.Kernel.MongoDB.Sinks;

#pragma warning disable CA1849, MA0042 // MongoDB breaks the Orleans task model internally, so it won't return to the task scheduler

/// <summary>
/// Represents an implementation of <see cref="ISink"/> for working with projections in MongoDB.
/// </summary>
public class MongoDBSink : ISink, IDisposable
{
    readonly Model _model;
    readonly IMongoDBConverter _converter;
    readonly IMongoDBSinkCollections _collections;
    readonly IExpandoObjectConverter _expandoObjectConverter;

    /// <inheritdoc/>
    public SinkTypeName Name => "MongoDB";

    /// <inheritdoc/>
    public SinkTypeId TypeId => WellKnownSinkTypes.MongoDB;


    /// <summary>
    /// Initializes a new instance of the <see cref="MongoDBSink"/> class.
    /// </summary>
    /// <param name="model">The <see cref="Model"/> the sink is for.</param>
    /// <param name="converter"><see cref="IMongoDBConverter"/> for dealing with conversion.</param>
    /// <param name="collections"></param>
    /// <param name="expandoObjectConverter"><see cref="IExpandoObjectConverter"/> for converting between documents and <see cref="ExpandoObject"/>.</param>
    public MongoDBSink(
        Model model,
        IMongoDBConverter converter,
        IMongoDBSinkCollections collections,
        IExpandoObjectConverter expandoObjectConverter)
    {
        _expandoObjectConverter = expandoObjectConverter;
        _model = model;
        _converter = converter;
        _collections = collections;
    }

    /// <inheritdoc/>
    public async Task<ExpandoObject?> FindOrDefault(Key key, bool isReplaying)
    {
        var collection = _collections.GetCollection(isReplaying);

        var result = await collection.FindAsync(Builders<BsonDocument>.Filter.Eq("_id", _converter.ToBsonValue(key)));
        var instance = result.SingleOrDefault();
        if (instance != default)
        {
            return _expandoObjectConverter.ToExpandoObject(instance, _model.Schema);
        }

        return default;
    }

    /// <inheritdoc/>
    public async Task ApplyChanges(Key key, IChangeset<AppendedEvent, ExpandoObject> changeset, bool isReplaying)
    {
        var updateDefinitionBuilder = Builders<BsonDocument>.Update;
        UpdateDefinition<BsonDocument>? updateBuilder = default;
        var hasChanges = false;

        var filter = Builders<BsonDocument>.Filter.Eq("_id", _converter.ToBsonValue(key));
        var collection = _collections.GetCollection(isReplaying);

        if (changeset.HasBeenRemoved())
        {
            await collection.DeleteOneAsync(filter);
            return;
        }

        var arrayFiltersForDocument = new List<BsonDocumentArrayFilterDefinition<BsonDocument>>();
        await ApplyActualChanges(key, changeset.Changes, updateDefinitionBuilder, ref updateBuilder, ref hasChanges, arrayFiltersForDocument, isReplaying);
        var distinctArrayFilters = arrayFiltersForDocument.DistinctBy(_ => _.Document).ToArray();

        if (!hasChanges) return;

        // var rendered = updateBuilder!.Render(BsonSerializer.LookupSerializer<BsonDocument>(), BsonSerializer.SerializerRegistry);
        await collection.UpdateOneAsync(
            filter,
            updateBuilder,
            new UpdateOptions
            {
                IsUpsert = true,
                ArrayFilters = distinctArrayFilters
            });
    }

    /// <inheritdoc/>
    public Task PrepareInitialRun(bool isReplaying) => _collections.PrepareInitialRun(isReplaying);

    /// <inheritdoc/>
    public Task BeginReplay() => _collections.BeginReplay();

    /// <inheritdoc/>
    public Task EndReplay() => _collections.EndReplay();

    /// <inheritdoc/>
    public void Dispose()
    {
        GC.SuppressFinalize(this);
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

                        hasChanges = propertiesChanged.Differences.Any();
                    }
                    break;

                case ChildAdded childAdded:
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

                        hasChanges = true;
                    }
                    break;

                case Joined joined:
                    {
                        var (property, arrayFilters) = _converter.ToMongoDBProperty(joined.OnProperty, joined.ArrayIndexers);

                        UpdateDefinition<BsonDocument>? joinUpdateBuilder = default;
                        var hasJoinChanges = false;

                        var serializedKey = _converter.ToBsonValue(key.Value);
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

}
