// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Auditing;
using Cratis.Chronicle.Concepts.Identities;

namespace Cratis.Chronicle.Concepts.Events.for_EventContext;

public class when_creating_copy_with_new_desired_state : Specification
{
    EventContext _original;
    EventContext _copy;

    void Establish() => _original = new(
        EventType.Unknown,
        EventSourceType.Default,
        Guid.NewGuid().ToString(),
        Guid.NewGuid().ToString(),
        Guid.NewGuid().ToString(),
        42,
        DateTimeOffset.UtcNow,
        Guid.NewGuid().ToString(),
        Guid.NewGuid().ToString(),
        CorrelationId.New(),
        [new Causation(DateTimeOffset.UtcNow, "Something", new Dictionary<string, string>() { { "prop", "42" } })],
        Identity.System,
        ["First Tag", "Second Tag"]);

    void Because() => _copy = _original.WithState(EventObservationState.Replay);

    [Fact] void should_be_a_new_object() => _copy.ShouldNotBeSame(_original);
    [Fact] void should_have_same_event_source_id() => _copy.EventSourceId.ShouldEqual(_original.EventSourceId);
    [Fact] void should_have_same_event_stream_type() => _copy.EventStreamType.ShouldEqual(_original.EventStreamType);
    [Fact] void should_have_same_event_stream_id() => _copy.EventStreamId.ShouldEqual(_original.EventStreamId);
    [Fact] void should_have_same_event_sequence_number() => _copy.SequenceNumber.ShouldEqual(_original.SequenceNumber);
    [Fact] void should_have_same_occurred() => _copy.Occurred.ShouldEqual(_original.Occurred);
    [Fact] void should_have_same_event_store() => _copy.EventStore.ShouldEqual(_original.EventStore);
    [Fact] void should_have_same_namespace() => _copy.Namespace.ShouldEqual(_original.Namespace);
    [Fact] void should_have_same_correlation_id() => _copy.CorrelationId.ShouldEqual(_original.CorrelationId);
    [Fact] void should_have_same_causation() => _copy.Causation.ShouldEqual(_original.Causation);
    [Fact] void should_have_same_caused_by() => _copy.CausedBy.ShouldEqual(_original.CausedBy);
    [Fact] void should_have_same_tags() => _copy.Tags.ShouldEqual(_original.Tags);
    [Fact] void should_have_new_state() => _copy.ObservationState.ShouldEqual(EventObservationState.Replay);
}
