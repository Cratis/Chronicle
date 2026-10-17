// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventSequences;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;

namespace Cratis.Chronicle.Storage.MongoDB.Keys.for_ObserverKeyIndexes.given;

/// <summary>
/// Sets up the key indexes of a namespace over an event collection that records the filter its keys are asked for with.
/// </summary>
public class a_namespace_collection : Specification
{
    protected ObserverKeyIndexes _indexes;
    protected IMongoCollection<Event> _collection;
    protected FilterDefinition<Event> _filter;

    void Establish()
    {
        var cursor = Substitute.For<IAsyncCursor<EventSourceId>>();
        cursor.MoveNextAsync(Arg.Any<CancellationToken>()).Returns(false);

        _collection = Substitute.For<IMongoCollection<Event>>();
        _collection
            .DistinctAsync(
                Arg.Any<FieldDefinition<Event, EventSourceId>>(),
                Arg.Any<FilterDefinition<Event>>(),
                Arg.Any<DistinctOptions>(),
                Arg.Any<CancellationToken>())
            .Returns(call =>
            {
                _filter = call.Arg<FilterDefinition<Event>>();
                return Task.FromResult(cursor);
            });

        var database = Substitute.For<IEventStoreNamespaceDatabase>();
        database.GetEventSequenceCollectionFor(Arg.Any<EventSequenceId>()).Returns(_collection);
        _indexes = new(database);
    }

    protected BsonDocument RenderedFilter => _filter.Render(new RenderArgs<Event>(BsonSerializer.LookupSerializer<Event>(), BsonSerializer.SerializerRegistry)).AsBsonDocument;
}
