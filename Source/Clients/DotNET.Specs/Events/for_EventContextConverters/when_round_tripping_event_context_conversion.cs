// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Auditing;
using Cratis.Chronicle.Identities;

namespace Cratis.Chronicle.Events.for_EventContextConverters;

public class when_round_tripping_event_context_conversion : Specification
{
    EventContext _original;
    EventContext _result;

    void Establish()
    {
        var properties = new Dictionary<string, string> { { "key", "value" } };
        _original = new(
            new("SomeEventType", 1),
            "SomeSourceType",
            "SomeSourceId",
            "SomeStreamType",
            "SomeStreamId",
            42,
            DateTimeOffset.UtcNow,
            "SomeEventStore",
            "SomeNamespace",
            CorrelationId.New(),
            [new Causation(DateTimeOffset.UtcNow, "causationType", properties)],
            new Identity("TheSubject", "TheUserName"),
            [],
            EventHash.NotSet,
            EventObservationState.Initial
        );
    }

    void Because() => _result = _original.ToContract().ToClient();

    [Fact] void should_preserve_event_type() => _result.EventType.ShouldEqual(_original.EventType);
    [Fact] void should_preserve_event_source_type() => _result.EventSourceType.ShouldEqual(_original.EventSourceType);
    [Fact] void should_preserve_event_source_id() => _result.EventSourceId.ShouldEqual(_original.EventSourceId);
    [Fact] void should_preserve_event_stream_type() => _result.EventStreamType.ShouldEqual(_original.EventStreamType);
    [Fact] void should_preserve_event_stream_id() => _result.EventStreamId.ShouldEqual(_original.EventStreamId);
    [Fact] void should_preserve_sequence_number() => _result.SequenceNumber.ShouldEqual(_original.SequenceNumber);
    [Fact] void should_preserve_occurred() => _result.Occurred.ShouldEqual(_original.Occurred);
    [Fact] void should_preserve_event_store() => _result.EventStore.ShouldEqual(_original.EventStore);
    [Fact] void should_preserve_namespace() => _result.Namespace.ShouldEqual(_original.Namespace);
    [Fact] void should_preserve_correlation_id() => _result.CorrelationId.ShouldEqual(_original.CorrelationId);
    [Fact] void should_preserve_causation() => _result.Causation.First().ShouldEqual(_original.Causation.First());
    [Fact] void should_preserve_caused_by() => _result.CausedBy.ShouldEqual(_original.CausedBy);
    [Fact] void should_preserve_observation_state() => _result.ObservationState.ShouldEqual(_original.ObservationState);
}
