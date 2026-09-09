// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using ProtoBuf;

namespace Cratis.Chronicle.Events.for_EventContextConverters;

/// <summary>
/// The sequence readback contract is a separate transport shape from the notification contract, so it needs its
/// own proof that an explicit subject survives serialization.
/// </summary>
public class when_round_tripping_a_sequence_event_context_through_the_wire_format : Specification
{
    const string TheEventSourceId = "the-stream";
    const string TheSubject = "the-person";

    Contracts.Sequences.EventContext _contract;
    EventContext _result;

    void Establish() => _contract = new()
    {
        EventType = new() { Id = "SomeEventType", Generation = 1 },
        EventSourceType = EventSourceType.Default,
        EventSourceId = TheEventSourceId,
        EventStreamType = EventStreamType.All,
        EventStreamId = EventStreamId.Default,
        SequenceNumber = 42,
        Occurred = DateTimeOffset.UtcNow,
        CorrelationId = Guid.NewGuid(),
        Causation = [],
        CausedBy = new Contracts.Sequences.Identity(),
        Tags = [],
        Hash = string.Empty,
        Subject = TheSubject
    };

    void Because()
    {
        using var stream = new MemoryStream();
        Serializer.Serialize(stream, _contract);
        stream.Position = 0;
        _result = Serializer.Deserialize<Contracts.Sequences.EventContext>(stream).ToClient("SomeEventStore", "SomeNamespace");
    }

    [Fact] void should_carry_the_subject_across_the_wire() => _result.Subject.Value.ShouldEqual(TheSubject);
    [Fact] void should_keep_the_event_source_id_distinct_from_the_subject() => _result.EventSourceId.Value.ShouldEqual(TheEventSourceId);
}
