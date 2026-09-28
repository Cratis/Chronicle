// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Storage.EventSequences;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.EventSequences.for_EventSequenceStorage.given;

/// <summary>Three events, including a legacy untagged event and two with different named tags.</summary>
public class a_storage_with_named_tags : an_event_sequence_storage
{
    protected static readonly DateTimeOffset _occurred = new(2025, 5, 5, 12, 0, 0, TimeSpan.Zero);

    async Task Establish()
    {
        var result = await _storage.AppendManyWithNamedTags(
        [
            Event(0, "one", [new(new TagName("account"), "a:b"), new(new TagName("project"), "42")]),
            Event(1, "two", [new(new TagName("account"), "b"), new(new TagName("project"), "99")]),
            Event(2, "three", [])
        ]);
        result.IsSuccess.ShouldBeTrue();
    }

    static EventToAppendToStorage Event(ulong number, string source, IReadOnlyCollection<NamedTag> tags) =>
        new(new EventSequenceNumber(number), EventSourceType.Default, new EventSourceId(source), EventStreamType.All, EventStreamId.Default, _eventType, CorrelationId.New(), [], [], [], _occurred, new ExpandoObject(), EventHash.NotSet) { NamedTags = tags };
}
