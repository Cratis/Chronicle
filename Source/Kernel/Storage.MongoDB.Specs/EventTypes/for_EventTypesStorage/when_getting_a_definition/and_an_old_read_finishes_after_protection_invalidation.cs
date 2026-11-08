// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.EventTypes;
using Cratis.Chronicle.Schemas;
using MongoDB.Bson;
using MongoDB.Driver;

using MongoEventType = Cratis.Chronicle.Storage.MongoDB.Events.EventTypes.EventType;

namespace Cratis.Chronicle.Storage.MongoDB.EventTypes.for_EventTypesStorage.when_getting_a_definition;

public class and_an_old_read_finishes_after_protection_invalidation : given.a_mocked_event_types_storage
{
    TaskCompletionSource<IAsyncCursor<MongoEventType>> _pendingRead;
    MongoEventType _oldDocument;
    EventTypeSchemaCache _cache;
    JsonSchema _retainedOwn;
    JsonSchema _result;
    JsonSchema _warm;

    async Task Establish()
    {
        _retainedOwn = await JsonSchema.FromJsonAsync("""{"type":"object","properties":{"name":{"type":"string"}}}""");
        _oldDocument = DocumentFor(_retainedOwn.ToJson());
        _eventTypesInDatabase.Add(_oldDocument);
        _pendingRead = new(TaskCreationOptions.RunContinuationsAsynchronously);
        var reads = 0;
        _collection.FindAsync<MongoEventType>(Arg.Any<FilterDefinition<MongoEventType>>(), null, default)
            .Returns(_ => ++reads == 1 ? _pendingRead.Task : Task.FromResult(CursorFor(_eventTypesInDatabase[0])));
        var storage = Substitute.For<IStorage>();
        storage.GetEventStore(EventStoreName.NotSet).EventTypes.Returns(_storage);
        _cache = new EventTypeSchemaCache(storage);
    }

    async Task Because()
    {
        var pending = _cache.GetProtectionSchemaFor(EventStoreName.NotSet, _eventTypeId, _firstGeneration, _retainedOwn).AsTask();
        _eventTypesInDatabase[0] = DocumentFor("""{"type":"object","properties":{"name":{"type":"string","compliance":[{"metadataType":"PII","details":""}]}}}""");

        // Match the cache grain service's storage-then-schema invalidation order.
        _storage.Invalidate(_eventTypeId);
        _cache.Invalidate(EventStoreName.NotSet, _eventTypeId);
        _pendingRead.SetResult(CursorFor(_oldDocument));
        _result = await pending;
        _warm = await _cache.GetProtectionSchemaFor(EventStoreName.NotSet, _eventTypeId, _firstGeneration, _retainedOwn);
    }

    [Fact] void should_not_publish_a_stale_negative_decision() => _result.HasSchemaMetadata().ShouldBeTrue();
    [Fact] void should_not_return_the_retained_plain_schema() => ReferenceEquals(_retainedOwn, _result).ShouldBeFalse();
    [Fact] void should_keep_current_protection_warm() => ReferenceEquals(_result, _warm).ShouldBeTrue();

    static MongoEventType DocumentFor(string schema) => new(_eventTypeId, EventTypeOwner.Client, EventTypeSource.Code, false, new Dictionary<string, BsonDocument> { { _firstGeneration.ToString(), BsonDocument.Parse(schema) } });

    static IAsyncCursor<MongoEventType> CursorFor(MongoEventType document)
    {
        var cursor = Substitute.For<IAsyncCursor<MongoEventType>>();
        cursor.MoveNextAsync(default).Returns(true, false);
        cursor.Current.Returns([document]);
        return cursor;
    }
}
