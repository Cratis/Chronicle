// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.EventTypes;
using Cratis.Chronicle.Schemas;

namespace Cratis.Chronicle.Storage.Sql.EventStores.EventTypes.for_EventTypesStorage.when_getting_a_definition;

public class and_an_old_read_finishes_after_protection_invalidation : given.an_event_types_storage
{
    TaskCompletionSource<DbContextScope<EventStoreDbContext>> _pendingRead;
    EventStoreDbContext _oldContext;
    EventTypeSchemaCache _cache;
    JsonSchema _retainedOwn;
    JsonSchema _result;
    JsonSchema _warm;

    async Task Establish()
    {
        _oldContext = CreateContext();
        var oldDocument = await _oldContext.EventTypes.FindAsync(_eventTypeId);
        _retainedOwn = await JsonSchema.FromJsonAsync(oldDocument!.Schemas[_firstGeneration]);
        _pendingRead = new(TaskCreationOptions.RunContinuationsAsynchronously);
        var reads = 0;
        _database.EventStore(Arg.Any<EventStoreName>())
            .Returns(_ => ++reads == 1 ? _pendingRead.Task : Task.FromResult(new DbContextScope<EventStoreDbContext>(CreateContext(), () => { })));
        var storage = Substitute.For<IStorage>();
        storage.GetEventStore(EventStoreName.NotSet).EventTypes.Returns(_storage);
        _cache = new EventTypeSchemaCache(storage);
    }

    async Task Because()
    {
        var pending = _cache.GetProtectionSchemaFor(EventStoreName.NotSet, _eventTypeId, _firstGeneration, _retainedOwn).AsTask();
        await using (var context = CreateContext())
        {
            var document = await context.EventTypes.FindAsync(_eventTypeId);
            document!.Schemas = new Dictionary<uint, string>
            {
                { _firstGeneration, "{\"type\":\"object\",\"properties\":{\"" + FirstGenerationProperty + "\":{\"type\":\"string\",\"compliance\":[{\"metadataType\":\"PII\",\"details\":\"\"}]}}}" }
            };
            await context.SaveChangesAsync();
        }

        // Match the grain service: evict storage first, then effective protection. The pending
        // query's tracking context still returns the old entity even though registration finished.
        _storage.Invalidate(_eventTypeId);
        _cache.Invalidate(EventStoreName.NotSet, _eventTypeId);
        _pendingRead.SetResult(new DbContextScope<EventStoreDbContext>(_oldContext, () => { }));
        _result = await pending;
        _warm = await _cache.GetProtectionSchemaFor(EventStoreName.NotSet, _eventTypeId, _firstGeneration, _retainedOwn);
    }

    [Fact] void should_not_publish_a_stale_negative_decision() => _result.HasSchemaMetadata().ShouldBeTrue();
    [Fact] void should_not_return_the_retained_plain_own_schema() => ReferenceEquals(_retainedOwn, _result).ShouldBeFalse();
    [Fact] void should_keep_the_current_protection_warm() => ReferenceEquals(_result, _warm).ShouldBeTrue();
}
