// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Storage.EventSequences;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.EventSequences.for_EventSequenceStorage.when_appending;

public class and_named_tags_are_rejected : given.an_event_sequence_storage
{
    Exception _exception;
    EventCount _count;

    async Task Because()
    {
        try
        {
            await ((IEventSequenceStorage)_storage).Append(
                EventSequenceNumber.First,
                EventSourceType.Default,
                "source",
                EventStreamType.All,
                EventStreamId.Default,
                _eventType,
                CorrelationId.New(),
                [],
                [],
                [],
                DateTimeOffset.UtcNow,
                new Dictionary<EventTypeGeneration, ExpandoObject> { [EventTypeGeneration.First] = new ExpandoObject() },
                new Dictionary<EventTypeGeneration, EventHash>(),
                null,
                [new NamedTag(new TagName("account"), "one")]);
        }
        catch (Exception exception)
        {
            _exception = exception;
        }

        _count = await _storage.GetCount();
    }

    [Fact] void should_reject_named_tags() => _exception.ShouldBeOfExactType<NamedTagsNotSupported>();
    [Fact] void should_not_write() => _count.Value.ShouldEqual(0UL);
}
