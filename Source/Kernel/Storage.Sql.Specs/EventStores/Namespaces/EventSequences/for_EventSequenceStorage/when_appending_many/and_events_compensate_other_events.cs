// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Storage.EventSequences;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.EventSequences.for_EventSequenceStorage.when_appending_many;

public class and_events_compensate_other_events : given.an_event_sequence_storage
{
    AppendedEvent[] _appended;
    AppendedEvent _compensation;
    AppendedEvent _ordinary;

    async Task Because()
    {
        var result = await _storage.AppendMany([new EventToAppendToStorage(
            EventSequenceNumber.First, EventSourceType.Default, "source", EventStreamType.All, EventStreamId.Default, _eventType, CorrelationId.New(), [], [], [], DateTimeOffset.UtcNow, new ExpandoObject(), EventHash.NotSet), new EventToAppendToStorage(
            new EventSequenceNumber(1), EventSourceType.Default, "source", EventStreamType.All, EventStreamId.Default, _eventType, CorrelationId.New(), [], [], [], DateTimeOffset.UtcNow, new ExpandoObject(), EventHash.NotSet)
        {
            CompensationFor = EventSequenceNumber.First
        }]);
        _appended = result.AsT0.ToArray();
        _ordinary = await _storage.GetEventAt(EventSequenceNumber.First);
        _compensation = await _storage.GetEventAt(new EventSequenceNumber(1));
    }

    [Fact] void should_return_the_compensated_sequence_number_when_appending() => _appended[1].Context.CompensationFor.ShouldEqual(EventSequenceNumber.First);
    [Fact] void should_read_the_compensated_sequence_number_back() => _compensation.Context.CompensationFor.ShouldEqual(EventSequenceNumber.First);
    [Fact] void should_read_an_ordinary_event_without_a_compensated_sequence_number() => _ordinary.Context.CompensationFor.ShouldBeNull();
}
