// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Storage.EventSequences;

namespace Cratis.Chronicle.Storage.InMemory.EventSequences.for_EventSequenceStorage.when_appending_many;

public class and_building_an_event_fails : given.an_event_sequence_storage
{
    Exception _error;
    async Task Because()
    {
        dynamic cyclic = new ExpandoObject();
        cyclic.self = cyclic;
        _error = await Catch.Exception(() => _storage.AppendMany([EventAt(0UL, new ExpandoObject()), EventAt(1UL, cyclic)]));
    }
    [Fact] void should_report_not_committed() => _error.ShouldBeOfExactType<EventsNotCommitted>();
    [Fact] void should_preserve_an_empty_log() => _storage.Events.ShouldBeEmpty();
    EventToAppendToStorage EventAt(EventSequenceNumber number, ExpandoObject content) => new(number, EventSourceType.Default, "source", EventStreamType.All, EventStreamId.Default, _eventType, CorrelationId.New(), [], [], [], DateTimeOffset.UtcNow, content, EventHash.NotSet);
}
