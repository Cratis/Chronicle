// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Storage.EventSequences;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.EventSequences.for_EventSequenceStorage.when_appending_many;

public class and_events_belong_to_an_event_source : given.an_event_sequence_storage
{
    AppendedEvent[] _appended;
    AppendedEvent _through;
    AppendedEvent _without;

    async Task Because()
    {
        var result = await _storage.AppendMany([new EventToAppendToStorage(
            EventSequenceNumber.First, EventSourceType.Default, "source", EventStreamType.All, EventStreamId.Default, _eventType, CorrelationId.New(), [], [], [], DateTimeOffset.UtcNow, new ExpandoObject(), EventHash.NotSet)
        {
            EventSource = "ShoppingCart"
        }, new EventToAppendToStorage(
            new EventSequenceNumber(1), EventSourceType.Default, "source", EventStreamType.All, EventStreamId.Default, _eventType, CorrelationId.New(), [], [], [], DateTimeOffset.UtcNow, new ExpandoObject(), EventHash.NotSet)]);
        _appended = result.AsT0.ToArray();
        _through = await _storage.GetEventAt(EventSequenceNumber.First);
        _without = await _storage.GetEventAt(new EventSequenceNumber(1));
    }

    [Fact] void should_return_the_event_source_when_appending() => _appended[0].Context.EventSource.Value.ShouldEqual("ShoppingCart");
    [Fact] void should_read_the_event_source_back() => _through.Context.EventSource.Value.ShouldEqual("ShoppingCart");
    [Fact] void should_read_an_event_without_an_event_source_as_not_set() => _without.Context.EventSource.ShouldEqual(EventSourceName.NotSet);
}
