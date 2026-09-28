// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Storage.EventSequences;

namespace Cratis.Chronicle.Storage.InMemory.EventSequences.for_EventSequenceStorage.when_appending_many;

public class and_events_have_distinct_named_tags : given.an_event_sequence_storage
{
    AppendedEvent _first;
    AppendedEvent _second;

    async Task Because()
    {
        var events = new[] { "one", "two" }.Select((value, index) => new EventToAppendToStorage(
            new EventSequenceNumber((ulong)index),
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
            new ExpandoObject(),
            EventHash.NotSet)
        {
            NamedTags = [new NamedTag(new TagName("account"), value)]
        });
        await _storage.AppendManyWithNamedTags(events);
        _first = await _storage.GetEventAt(EventSequenceNumber.First);
        _second = await _storage.GetEventAt(new EventSequenceNumber(1));
    }

    [Fact] void should_keep_first_event_named_tags() => _first.Context.NamedTags.Single().Value.ShouldEqual("one");
    [Fact] void should_keep_second_event_named_tags() => _second.Context.NamedTags.Single().Value.ShouldEqual("two");
}
