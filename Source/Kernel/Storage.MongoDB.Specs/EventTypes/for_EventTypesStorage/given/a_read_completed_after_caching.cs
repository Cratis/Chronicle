// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using MongoDB.Bson;
using MongoDB.Driver;

using MongoEventType = Cratis.Chronicle.Storage.MongoDB.Events.EventTypes.EventType;

namespace Cratis.Chronicle.Storage.MongoDB.EventTypes.for_EventTypesStorage.given;

public class a_read_completed_after_caching : a_mocked_event_types_storage
{
    TaskCompletionSource<IAsyncCursor<MongoEventType>> _pendingRead;

    void Establish()
    {
        _eventTypesInDatabase.Add(new MongoEventType(
            _eventTypeId,
            EventTypeOwner.Client,
            EventTypeSource.Code,
            false,
            new Dictionary<string, BsonDocument> { { _firstGeneration.ToString(), BsonDocument.Parse("{ 'type': 'object' }") } }));

        _pendingRead = new(TaskCreationOptions.RunContinuationsAsynchronously);
        var reads = 0;
        _collection
            .FindAsync<MongoEventType>(Arg.Any<FilterDefinition<MongoEventType>>(), null, default)
            .Returns(_ => ++reads == 1 ? _pendingRead.Task : Task.FromResult(CursorFor(_eventTypesInDatabase[0])));
    }

    protected void CompleteRead()
    {
        // A competing read has already populated the cache. This stale document cannot be converted,
        // so returning the cached instance also proves that the losing read never parses it.
        var staleDocument = new MongoEventType(
            _eventTypeId,
            EventTypeOwner.Client,
            EventTypeSource.Code,
            false,
            new Dictionary<string, BsonDocument> { { "invalid-generation", new BsonDocument("type", 42) } });
        _pendingRead.SetResult(CursorFor(staleDocument));
    }

    static IAsyncCursor<MongoEventType> CursorFor(MongoEventType document)
    {
        var cursor = Substitute.For<IAsyncCursor<MongoEventType>>();
        cursor.MoveNextAsync(default).Returns(true, false);
        cursor.Current.Returns([document]);
        return cursor;
    }
}
