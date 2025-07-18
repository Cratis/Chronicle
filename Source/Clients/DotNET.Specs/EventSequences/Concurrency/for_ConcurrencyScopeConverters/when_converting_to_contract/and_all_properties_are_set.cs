// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.EventSequences.Concurrency.for_ConcurrencyScopeConverters.when_converting_to_contract;

public class and_all_properties_are_set : Specification
{
    ConcurrencyScope _scope;
    Contracts.EventSequences.Concurrency.ConcurrencyScope _result;

    void Establish()
    {
        _scope = new ConcurrencyScope(
            new EventSequenceNumber(42),
            new EventSourceId("some-event-source-id"),
            new EventStreamType("SomeStreamType"),
            new EventStreamId("some-stream-id"),
            new EventSourceType("SomeSourceType"),
            [
                new EventType("SomeEventType", 1),
                new EventType("AnotherEventType", 2)
            ]);
    }

    void Because() => _result = _scope.ToContract();

    [Fact] void should_set_sequence_number() => _result.EventSequenceNumber.ShouldEqual(42ul);
    [Fact] void should_set_event_source_id_to_true() => _result.EventSourceId.ShouldBeTrue();
    [Fact] void should_set_event_stream_type() => _result.EventStreamType.ShouldEqual("SomeStreamType");
    [Fact] void should_set_event_stream_id() => _result.EventStreamId.ShouldEqual("some-stream-id");
    [Fact] void should_set_event_source_type() => _result.EventSourceType.ShouldEqual("SomeSourceType");
    [Fact] void should_set_event_types() => _result.EventTypes.ShouldContain("SomeEventType+1");
    [Fact] void should_set_all_event_types() => _result.EventTypes.ShouldContain("AnotherEventType+2");
    [Fact] void should_have_correct_event_types_count() => _result.EventTypes.Count.ShouldEqual(2);
}
