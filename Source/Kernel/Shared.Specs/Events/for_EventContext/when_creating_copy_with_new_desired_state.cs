// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Auditing;
using Cratis.Chronicle.Identities;

namespace Cratis.Chronicle.Events.for_EventContext;

public class when_creating_copy_with_new_desired_state : Specification
{
    EventContext original;
    EventContext copy;

    void Establish() => original = new(
        Guid.NewGuid().ToString(),
        42,
        DateTimeOffset.UtcNow,
        Guid.NewGuid().ToString(),
        Guid.NewGuid().ToString(),
        CorrelationId.New(),
        [new Causation(DateTimeOffset.UtcNow, "Something", new Dictionary<string, string>() { { "prop", "42" } })],
        Identity.System);

    void Because() => copy = original.WithState(EventObservationState.Replay);

    [Fact] void should_be_a_new_object() => copy.ShouldNotBeSame(original);
    [Fact] void should_have_same_event_source_id() => copy.EventSourceId.ShouldEqual(original.EventSourceId);
    [Fact] void should_have_same_event_sequence_number() => copy.SequenceNumber.ShouldEqual(original.SequenceNumber);
    [Fact] void should_have_same_occurred() => copy.Occurred.ShouldEqual(original.Occurred);
    [Fact] void should_have_same_event_store() => copy.EventStore.ShouldEqual(original.EventStore);
    [Fact] void should_have_same_namespace() => copy.Namespace.ShouldEqual(original.Namespace);
    [Fact] void should_have_same_correlation_id() => copy.CorrelationId.ShouldEqual(original.CorrelationId);
    [Fact] void should_have_same_causation() => copy.Causation.ShouldEqual(original.Causation);
    [Fact] void should_have_same_caused_by() => copy.CausedBy.ShouldEqual(original.CausedBy);
    [Fact] void should_have_new_state() => copy.ObservationState.ShouldEqual(EventObservationState.Replay);
}
