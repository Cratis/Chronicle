// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.Sequences.for_EventContext;

public class when_using_the_constructor_and_deconstruction_shape : Specification
{
    EventContext _context;

    EventType _eventType;
    string _eventSourceType;
    string _eventSourceId;
    ulong _sequenceNumber;
    string _eventStreamType;
    string _eventStreamId;
    DateTimeOffset _occurred;
    Guid _correlationId;
    IEnumerable<Causation> _causation;
    Identity _causedBy;
    IEnumerable<string> _tags;
    EventHash _hash;
    EventObservationState _observationState;

    void Establish() => _context = new EventContext(
        new EventType("SomeEventType", 1, false),
        "SomeSourceType",
        "SomeSourceId",
        42,
        "SomeStreamType",
        "SomeStreamId",
        DateTimeOffset.UtcNow,
        Guid.NewGuid(),
        [],
        new Identity("SomeSubject", "SomeName", "SomeUserName", null),
        [],
        EventHash.NotSet,
        EventObservationState.Initial);

    void Because() => (
        _eventType,
        _eventSourceType,
        _eventSourceId,
        _sequenceNumber,
        _eventStreamType,
        _eventStreamId,
        _occurred,
        _correlationId,
        _causation,
        _causedBy,
        _tags,
        _hash,
        _observationState) = _context;

    [Fact] void should_construct_without_a_subject_argument() => _context.ShouldNotBeNull();
    [Fact] void should_deconstruct_into_the_same_thirteen_values() => _eventSourceId.ShouldEqual("SomeSourceId");
    [Fact] void should_deconstruct_the_observation_state() => _observationState.ShouldEqual(EventObservationState.Initial);
    [Fact] void should_default_the_subject_to_empty_when_not_supplied() => _context.Subject.ShouldEqual(string.Empty);
    [Fact] void should_carry_a_subject_set_through_an_initializer() => (_context with { Subject = "person-42" }).Subject.ShouldEqual("person-42");
}
