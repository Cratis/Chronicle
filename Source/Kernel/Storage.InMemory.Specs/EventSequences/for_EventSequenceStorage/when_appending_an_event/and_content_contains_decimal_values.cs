// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.Storage.InMemory.EventSequences.for_EventSequenceStorage.when_appending_an_event;

public class and_content_contains_decimal_values : given.an_event_sequence_storage
{
    AppendedEvent _read;

    async Task Because()
    {
        dynamic content = new ExpandoObject();
        content.amount = 193.58m;
        content.precise = 1234567890.123456789012345678m;
        await _storage.Append(EventSequenceNumber.First, EventSourceType.Default, "source", EventStreamType.All, EventStreamId.Default, _eventType, CorrelationId.NotSet, [], [], [], DateTimeOffset.UtcNow, new Dictionary<EventTypeGeneration, ExpandoObject> { [EventTypeGeneration.First] = content }, new Dictionary<EventTypeGeneration, EventHash>());
        _read = await _storage.GetEventAt(EventSequenceNumber.First);
    }

    [Fact] void should_preserve_amount_bits() => decimal.GetBits((decimal)((IDictionary<string, object?>)_read.Content)["amount"]!).ShouldEqual(decimal.GetBits(193.58m));
    [Fact] void should_preserve_all_significant_digits() => decimal.GetBits((decimal)((IDictionary<string, object?>)_read.Content)["precise"]!).ShouldEqual(decimal.GetBits(1234567890.123456789012345678m));
}
