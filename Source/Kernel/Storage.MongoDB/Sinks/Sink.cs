// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Aksio.Cratis.Changes;
using Aksio.Cratis.Events;
using Aksio.Cratis.Kernel.Keys;
using Aksio.Cratis.Kernel.Storage.Sinks;
using Aksio.Cratis.Projections;
using Aksio.Cratis.Sinks;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Aksio.Cratis.Kernel.Storage.MongoDB.Sinks;

#pragma warning disable CA1849, MA0042 // MongoDB breaks the Orleans task model internally, so it won't return to the task scheduler

/// <summary>
/// Represents an implementation of <see cref="ISink"/> for working with projections in MongoDB.
/// </summary>
public class Sink : ISink
{
    readonly Model _model;
    readonly IMongoDBConverter _converter;
    readonly ISinkCollections _collections;
    readonly IChangesetConverter _changesetConverter;
    readonly IExpandoObjectConverter _expandoObjectConverter;

    bool _isReplaying;

    /// <summary>
    /// Initializes a new instance of the <see cref="Sink"/> class.
    /// </summary>
    /// <param name="model">The <see cref="Model"/> the sink is for.</param>
    /// <param name="converterProvider"><see cref="IMongoDBConverter"/> for dealing with conversion.</param>
    /// <param name="collections">Provider for <see cref="ISinkCollections"/> to use.</param>
    /// <param name="changesetConverter">Provider for <see cref="IChangesetConverter"/> for converting changesets.</param>
    /// <param name="expandoObjectConverter"><see cref="IExpandoObjectConverter"/> for converting between documents and <see cref="ExpandoObject"/>.</param>
    public Sink(
        Model model,
        IMongoDBConverter converterProvider,
        ISinkCollections collections,
        IChangesetConverter changesetConverter,
        IExpandoObjectConverter expandoObjectConverter)
    {
        _expandoObjectConverter = expandoObjectConverter;
        _model = model;
        _converter = converterProvider;
        _collections = collections;
        _changesetConverter = changesetConverter;
    }

    /// <inheritdoc/>
    public SinkTypeName Name => "MongoDB";

    /// <inheritdoc/>
    public SinkTypeId TypeId => WellKnownSinkTypes.MongoDB;

    IMongoCollection<BsonDocument> Collection => _collections.GetCollection();

    /// <inheritdoc/>
    public async Task<ExpandoObject?> FindOrDefault(Key key)
    {
        var result = await Collection.FindAsync(Builders<BsonDocument>.Filter.Eq("_id", _converter.ToBsonValue(key)));
        var instance = result.SingleOrDefault();
        if (instance != default)
        {
            return _expandoObjectConverter.ToExpandoObject(instance, _model.Schema);
        }

        return default;
    }

    /// <inheritdoc/>
    public async Task ApplyChanges(Key key, IChangeset<AppendedEvent, ExpandoObject> changeset)
    {
        var filter = Builders<BsonDocument>.Filter.Eq("_id", _converter.ToBsonValue(key));

        if (changeset.HasBeenRemoved())
        {
            await Collection.DeleteOneAsync(filter);
            return;
        }

        var converted = await _changesetConverter.ToUpdateDefinition(key, changeset, _isReplaying);

        if (!converted.hasChanges) return;

        await Collection.UpdateOneAsync(
            filter,
            converted.UpdateDefinition,
            new UpdateOptions
            {
                IsUpsert = true,
                ArrayFilters = converted.ArrayFilters
            });
    }

    /// <inheritdoc/>
    public Task PrepareInitialRun() => _collections.PrepareInitialRun();

    /// <inheritdoc/>
    public async Task BeginReplay()
    {
        _isReplaying = true;
        await _collections.BeginReplay();
    }

    /// <inheritdoc/>
    public async Task EndReplay()
    {
        await _collections.EndReplay();
        _isReplaying = true;
    }
}
