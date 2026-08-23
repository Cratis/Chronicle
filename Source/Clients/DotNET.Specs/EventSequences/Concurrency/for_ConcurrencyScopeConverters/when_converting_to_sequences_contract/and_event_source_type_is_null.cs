// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.EventSequences.Concurrency.for_ConcurrencyScopeConverters.when_converting_to_contract;

public class and_event_source_type_is_null : Specification
{
    ConcurrencyScope _scope;
    Contracts.EventSequences.Concurrency.ConcurrencyScope _result;
    EventType _eventType;

    void Establish()
    {
        _eventType = new("SomeEventType", 1);
        _scope = new ConcurrencyScope(
            new EventSequenceNumber(42),
            new EventSourceId("some-event-source-id"),
            new EventStreamType("SomeStreamType"),
            new EventStreamId("some-stream-id"),
            EventSourceType: null,
            [_eventType]);
    }

    void Because() => _result = _scope.ToContract();

    [Fact] void should_set_event_source_type_to_null() => _result.EventSourceType.ShouldBeNull();
    [Fact] void should_set_sequence_number() => _result.SequenceNumber.ShouldEqual(42ul);
    [Fact] void should_set_event_source_id_to_true() => _result.EventSourceId.ShouldBeTrue();
    [Fact] void should_set_event_stream_type() => _result.EventStreamType.ShouldEqual("SomeStreamType");
    [Fact] void should_set_event_stream_id() => _result.EventStreamId.ShouldEqual("some-stream-id");
    [Fact] void should_set_event_types() => _result.EventTypes.ShouldContain(et => et.Id == _eventType.Id && et.Generation == _eventType.Generation);
}
