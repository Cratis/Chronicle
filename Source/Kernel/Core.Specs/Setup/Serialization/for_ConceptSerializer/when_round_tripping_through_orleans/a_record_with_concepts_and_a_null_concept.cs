// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Clients;
using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.Setup.Serialization.for_ConceptSerializer.when_round_tripping_through_orleans;

public class a_record_with_concepts_and_a_null_concept : given.a_configured_orleans_serializer
{
    Envelope _original;
    Envelope _result;

    void Establish() => _original = new Envelope(ConnectionId.New(), "the-source", null);

    void Because() => _result = RoundTrip(_original);

    [Fact] void should_round_trip_the_guid_concept() => _result.ConnectionId.ShouldEqual(_original.ConnectionId);
    [Fact] void should_round_trip_the_string_concept() => _result.EventSourceId.ShouldEqual(_original.EventSourceId);
    [Fact] void should_round_trip_the_null_concept_as_null() => _result.Optional.ShouldBeNull();

    [GenerateSerializer]
    public record Envelope(ConnectionId ConnectionId, EventSourceId EventSourceId, EventSourceId? Optional);
}
