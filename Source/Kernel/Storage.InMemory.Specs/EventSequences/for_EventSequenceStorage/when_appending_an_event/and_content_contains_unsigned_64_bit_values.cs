// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.Storage.InMemory.EventSequences.for_EventSequenceStorage.when_appending_an_event;

public class and_content_contains_unsigned_64_bit_values : given.an_event_sequence_storage
{
    AppendedEvent _read;

    async Task Because()
    {
        var content = new ExpandoObject();
        var values = (IDictionary<string, object?>)content;
        values["value"] = ulong.MaxValue;
        values["values"] = new[] { ulong.MaxValue };
        await _storage.Append(
            EventSequenceNumber.First,
            EventSourceType.Default,
            "source",
            EventStreamType.All,
            EventStreamId.Default,
            _eventType,
            CorrelationId.NotSet,
            [],
            [],
            [],
            DateTimeOffset.UtcNow,
            new Dictionary<EventTypeGeneration, ExpandoObject> { [EventTypeGeneration.First] = content },
            new Dictionary<EventTypeGeneration, EventHash>());
        _read = await _storage.GetEventAt(EventSequenceNumber.First);
    }

    [Fact] void should_preserve_the_maximum_value() => ((IDictionary<string, object?>)_read.Content)["value"].ShouldEqual(ulong.MaxValue);
    [Fact] void should_preserve_array_elements() => ((ulong[])((IDictionary<string, object?>)_read.Content)["values"]!)[0].ShouldEqual(ulong.MaxValue);
}
