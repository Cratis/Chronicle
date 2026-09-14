// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using ProtoBuf;

namespace Cratis.Chronicle.Events.for_EventContextConverters;

/// <summary>
/// The legacy control for the sequence readback contract - an older server sends no subject member at all.
/// </summary>
public class when_converting_a_sequence_event_context_from_a_server_that_does_not_carry_a_subject : Specification
{
    const string TheEventSourceId = "the-stream";

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
        Hash = string.Empty
    };

    void Because()
    {
        using var stream = new MemoryStream();
        Serializer.Serialize(stream, _contract);
        stream.Position = 0;
        _result = Serializer.Deserialize<Contracts.Sequences.EventContext>(stream).ToClient("SomeEventStore", "SomeNamespace");
    }

    [Fact] void should_fall_back_to_the_event_source_id_as_subject() => _result.Subject.Value.ShouldEqual(TheEventSourceId);
    [Fact] void should_have_a_set_subject() => _result.Subject.IsSet.ShouldBeTrue();
}
