// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Storage.EventSequences;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.EventSequences.for_EventSequenceStorage.when_appending_many;

public class and_an_event_has_named_tags : given.an_event_sequence_storage
{
    AppendedEvent[] _appended;
    AppendedEvent _first;
    AppendedEvent _second;

    async Task Because()
    {
        var result = await _storage.AppendManyWithNamedTags([new EventToAppendToStorage(
            EventSequenceNumber.First, EventSourceType.Default, "source", EventStreamType.All, EventStreamId.Default, _eventType, CorrelationId.New(), [], [], [], DateTimeOffset.UtcNow, new ExpandoObject(), EventHash.NotSet)
        {
            NamedTags = [new NamedTag(new TagName("account"), "one"), new NamedTag(new TagName("account"), "one")]
        }, new EventToAppendToStorage(
            new EventSequenceNumber(1), EventSourceType.Default, "source", EventStreamType.All, EventStreamId.Default, _eventType, CorrelationId.New(), [], [], [], DateTimeOffset.UtcNow, new ExpandoObject(), EventHash.NotSet)]);
        _appended = result.AsT0.ToArray();
        _first = await _storage.GetEventAt(EventSequenceNumber.First);
        _second = await _storage.GetEventAt(new EventSequenceNumber(1));
    }

    [Fact] void should_return_per_event_tags() => _appended.Select(_ => _.Context.NamedTags.Count()).ShouldEqual([2, 0]);
    [Fact] void should_preserve_duplicate_tags_and_position() => _first.Context.NamedTags.Select(_ => _.Value).ShouldEqual(["one", "one"]);
    [Fact] void should_not_spill_tags_into_the_next_event() => _second.Context.NamedTags.ShouldBeEmpty();
}
