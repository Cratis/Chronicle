// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.Storage.InMemory.EventSequences.for_EventSequenceStorage.when_appending_an_event;

public class and_named_tags_are_read_back : given.an_event_sequence_storage
{
    AppendedEvent _appended;
    AppendedEvent _read;

    async Task Because()
    {
        var result = await _storage.Append(
            EventSequenceNumber.First,
            EventSourceType.Default,
            "some-source",
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
            [new NamedTag(new TagName("account"), "one"), new NamedTag(new TagName("account"), "two")]);
        _appended = result.AsT0;
        _read = await _storage.GetEventAt(EventSequenceNumber.First);
    }

    [Fact] void should_return_the_named_tags() => _appended.Context.NamedTags.Select(tag => tag.Value).ShouldContainOnly("one", "two");
    [Fact] void should_read_the_named_tags() => _read.Context.NamedTags.Select(tag => tag.Value).ShouldContainOnly("one", "two");
}
