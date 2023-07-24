// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Aksio.Cratis.Changes;
using Aksio.Cratis.Events;
using Aksio.Cratis.Kernel.Engines.Projections;
using Aksio.Cratis.Kernel.Engines.Sinks;
using Aksio.Cratis.Projections;
using Aksio.Cratis.Sinks;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Aksio.Cratis.Kernel.MongoDB.Sinks;

#pragma warning disable CA1849, MA0042 // MongoDB breaks the Orleans task model internally, so it won't return to the task scheduler

/// <summary>
/// Represents an implementation of <see cref="ISink"/> for working with projections in MongoDB.
/// </summary>
public class MongoDBSink : ISink
{
    readonly Model _model;
    readonly IMongoDBConverter _converter;
    readonly IMongoDBSinkCollections _collections;
    readonly IMongoDBChangesetConverter _changesetConverter;
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
    /// <param name="collections"><see cref="IMongoDBSinkCollections"/> to use.</param>
    /// <param name="changesetConverter"></param>
    /// <param name="expandoObjectConverter"><see cref="IExpandoObjectConverter"/> for converting between documents and <see cref="ExpandoObject"/>.</param>
    public MongoDBSink(
        Model model,
        IMongoDBConverter converter,
        IMongoDBSinkCollections collections,
        IMongoDBChangesetConverter changesetConverter,
        IExpandoObjectConverter expandoObjectConverter)
    {
        _expandoObjectConverter = expandoObjectConverter;
        _model = model;
        _converter = converter;
        _collections = collections;
        _changesetConverter = changesetConverter;
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
        var filter = Builders<BsonDocument>.Filter.Eq("_id", _converter.ToBsonValue(key));
        var collection = _collections.GetCollection(isReplaying);

        if (changeset.HasBeenRemoved())
        {
            await collection.DeleteOneAsync(filter);
            return;
        }

        var converted = await _changesetConverter.ToUpdateDefinition(key, changeset, isReplaying);

        if (!converted.hasChanges) return;

        await collection.UpdateOneAsync(
            filter,
            converted.UpdateDefinition,
            new UpdateOptions
            {
                IsUpsert = true,
                ArrayFilters = converted.ArrayFilters
            });
    }

    /// <inheritdoc/>
    public Task PrepareInitialRun(bool isReplaying) => _collections.PrepareInitialRun(isReplaying);

    /// <inheritdoc/>
    public Task BeginReplay() => _collections.BeginReplay();

    /// <inheritdoc/>
    public Task EndReplay() => _collections.EndReplay();
}
