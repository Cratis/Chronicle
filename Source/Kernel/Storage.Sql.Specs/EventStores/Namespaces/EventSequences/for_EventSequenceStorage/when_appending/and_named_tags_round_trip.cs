// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Storage.EventSequences;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.EventSequences.for_EventSequenceStorage.when_appending;

public class and_named_tags_round_trip : given.an_event_sequence_storage
{
    AppendedEvent _appended;
    AppendedEvent _readBack;

    async Task Because()
    {
        var result = await ((IEventSequenceStorage)_storage).Append(
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
        _appended = result.AsT0;
        _readBack = await _storage.GetEventAt(EventSequenceNumber.First);
    }

    [Fact] void should_include_the_named_tag_in_the_append_result() => _appended.Context.NamedTags.Single().Value.ShouldEqual("one");
    [Fact] void should_read_back_the_named_tag() => _readBack.Context.NamedTags.Single().Name.Value.ShouldEqual("account");
}
