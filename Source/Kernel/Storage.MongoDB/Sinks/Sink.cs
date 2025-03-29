// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Chronicle.Changes;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Concepts.Models;
using Cratis.Chronicle.Concepts.Sinks;
using Cratis.Chronicle.Storage.Sinks;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Cratis.Chronicle.Storage.MongoDB.Sinks;

#pragma warning disable CA1849, MA0042 // MongoDB breaks the Orleans task model internally, so it won't return to the task scheduler

/// <summary>
/// Represents an implementation of <see cref="ISink"/> for working with projections in MongoDB.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="Sink"/> class.
/// </remarks>
/// <param name="model">The <see cref="Model"/> the sink is for.</param>
/// <param name="converter"><see cref="IMongoDBConverter"/> for dealing with conversion.</param>
/// <param name="collections">Provider for <see cref="ISinkCollections"/> to use.</param>
/// <param name="changesetConverter">Provider for <see cref="IChangesetConverter"/> for converting changesets.</param>
/// <param name="expandoObjectConverter"><see cref="IExpandoObjectConverter"/> for converting between documents and <see cref="ExpandoObject"/>.</param>
public class Sink(
    Model model,
    IMongoDBConverter converter,
    ISinkCollections collections,
    IChangesetConverter changesetConverter,
    IExpandoObjectConverter expandoObjectConverter) : ISink
{
    /// <inheritdoc/>
    public SinkTypeName Name => "MongoDB";

    /// <inheritdoc/>
    public SinkTypeId TypeId => WellKnownSinkTypes.MongoDB;

    IMongoCollection<BsonDocument> Collection => collections.GetCollection();

    /// <inheritdoc/>
    public async Task<ExpandoObject?> FindOrDefault(Key key)
    {
        var collection = Collection;

        using var result = await collection.FindAsync(Builders<BsonDocument>.Filter.Eq("_id", converter.ToBsonValue(key)));
        var instance = result.SingleOrDefault();
        if (instance != default)
        {
            return expandoObjectConverter.ToExpandoObject(instance, model.Schema);
        }

        return default;
    }

    /// <inheritdoc/>
    public async Task ApplyChanges(
        Key key,
        IChangeset<AppendedEvent, ExpandoObject> changeset,
        EventSequenceNumber eventSequenceNumber)
    {
        var filter = changeset.HasJoined() ?
            FilterDefinition<BsonDocument>.Empty :
            Builders<BsonDocument>.Filter.Eq("_id", converter.ToBsonValue(key));

        if (changeset.HasBeenRemoved())
        {
            await Collection.DeleteOneAsync(filter);
            return;
        }

        // Run through and remove all children affected by ChildRemovedFromAll
        foreach (var childRemoved in changeset.Changes.OfType<ChildRemovedFromAll>())
        {
            await RemoveChildFromAll(key, childRemoved);
        }

        var converted = await changesetConverter.ToUpdateDefinition(key, changeset, eventSequenceNumber);
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
    public Task PrepareInitialRun() => collections.PrepareInitialRun();

    /// <inheritdoc/>
    public async Task BeginReplay(Chronicle.Storage.Sinks.ReplayContext context)
    {
        await collections.BeginReplay(context);
    }

    /// <inheritdoc/>
    public async Task ResumeReplay(Chronicle.Storage.Sinks.ReplayContext context)
    {
        await collections.ResumeReplay(context);
    }

    /// <inheritdoc/>
    public async Task EndReplay(Chronicle.Storage.Sinks.ReplayContext context)
    {
        await collections.EndReplay(context);
    }

    async Task RemoveChildFromAll(Key key, ChildRemovedFromAll childRemoved)
    {
        var childrenProperty = (string)childRemoved.ChildrenProperty.GetChildrenProperty();
        var identifiedByProperty = (string)childRemoved.IdentifiedByProperty;
        var propertyValue = key.Value.ToBsonValue();

        var collection = collections.GetCollection();

        var filter = Builders<BsonDocument>.Filter.Empty;
        var childFilter = Builders<BsonDocument>.Filter.Eq(identifiedByProperty, propertyValue);
        var update = Builders<BsonDocument>.Update.PullFilter(childrenProperty, childFilter);
        await collection.UpdateManyAsync(filter, update);
    }
}
