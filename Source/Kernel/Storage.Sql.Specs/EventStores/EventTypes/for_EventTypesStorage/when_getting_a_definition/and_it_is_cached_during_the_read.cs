// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.Storage.Sql.EventStores.EventTypes.for_EventTypesStorage.when_getting_a_definition;

public class and_it_is_cached_during_the_read : given.an_event_types_storage
{
    TaskCompletionSource<DbContextScope<EventStoreDbContext>> _pendingRead;
    EventTypeDefinition _cached;
    EventTypeDefinition _result;

    void Establish()
    {
        _pendingRead = new(TaskCreationOptions.RunContinuationsAsynchronously);
        var reads = 0;
        _database.EventStore(Arg.Any<EventStoreName>())
            .Returns(_ => ++reads == 1 ? _pendingRead.Task : Task.FromResult(new DbContextScope<EventStoreDbContext>(CreateContext(), () => { })));
    }

    async Task Because()
    {
        var pending = _storage.GetDefinition(_eventTypeId);
        _cached = await _storage.GetDefinition(_eventTypeId);

        // The losing read sees an unparseable schema, but the definition is already cached.
        await using (var context = CreateContext())
        {
            var eventType = await context.EventTypes.FindAsync(_eventTypeId);
            eventType!.Schemas = new Dictionary<uint, string> { { _firstGeneration, "not-json" } };
            await context.SaveChangesAsync();
        }

        _pendingRead.SetResult(new DbContextScope<EventStoreDbContext>(CreateContext(), () => { }));
        _result = await pending;
    }

    [Fact] void should_reuse_the_cached_definition_without_converting_the_stale_document() => _result.ShouldBeSame(_cached);
}
