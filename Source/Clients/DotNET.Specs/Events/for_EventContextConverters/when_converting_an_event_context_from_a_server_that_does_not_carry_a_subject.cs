// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Auditing;
using Cratis.Chronicle.Contracts.Identities;
using ProtoBuf;

namespace Cratis.Chronicle.Events.for_EventContextConverters;

/// <summary>
/// A server older than the subject member on the contract leaves it off the wire entirely. The subject it
/// implied was the event source id, so that is what the client must resolve - and only then.
/// </summary>
public class when_converting_an_event_context_from_a_server_that_does_not_carry_a_subject : Specification
{
    const string TheEventSourceId = "the-stream";

    Contracts.Events.EventContext _contract;
    EventContext _result;

    void Establish() => _contract = new()
    {
        EventType = new() { Id = "SomeEventType", Generation = 1 },
        EventSourceType = "SomeSourceType",
        EventSourceId = TheEventSourceId,
        EventStreamType = "SomeStreamType",
        EventStreamId = "SomeStreamId",
        SequenceNumber = 42,
        Occurred = DateTimeOffset.UtcNow,
        EventStore = "SomeEventStore",
        Namespace = "SomeNamespace",
        CorrelationId = Guid.NewGuid(),
        Causation = [new Causation { Occurred = DateTimeOffset.UtcNow, Type = "causationType", Properties = new Dictionary<string, string>() }],
        CausedBy = new Identity { Subject = string.Empty, Name = string.Empty, UserName = string.Empty },
        Tags = [],
        Hash = string.Empty,
        ObservationState = Contracts.Events.EventObservationState.Initial
    };

    void Because()
    {
        using var stream = new MemoryStream();
        Serializer.Serialize(stream, _contract);
        stream.Position = 0;
        _result = Serializer.Deserialize<Contracts.Events.EventContext>(stream).ToClient();
    }

    [Fact] void should_fall_back_to_the_event_source_id_as_subject() => _result.Subject.Value.ShouldEqual(TheEventSourceId);
    [Fact] void should_have_a_set_subject() => _result.Subject.IsSet.ShouldBeTrue();
}
