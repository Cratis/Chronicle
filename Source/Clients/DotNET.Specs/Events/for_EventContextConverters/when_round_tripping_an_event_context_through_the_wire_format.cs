// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Auditing;
using Cratis.Chronicle.Identities;
using ProtoBuf;

namespace Cratis.Chronicle.Events.for_EventContextConverters;

public class when_round_tripping_an_event_context_through_the_wire_format : Specification
{
    const string TheEventSourceId = "the-stream";
    const string TheSubject = "the-person";

    EventContext _original;
    EventContext _result;

    void Establish() => _original = new(
        new("SomeEventType", 1),
        "SomeSourceType",
        TheEventSourceId,
        "SomeStreamType",
        "SomeStreamId",
        42,
        DateTimeOffset.UtcNow,
        "SomeEventStore",
        "SomeNamespace",
        CorrelationId.New(),
        [new Causation(DateTimeOffset.UtcNow, "causationType", new Dictionary<string, string>())],
        Identity.NotSet,
        [],
        EventHash.NotSet,
        EventObservationState.Initial,
        new Subject(TheSubject));

    void Because()
    {
        using var stream = new MemoryStream();
        Serializer.Serialize(stream, _original.ToContract());
        stream.Position = 0;
        _result = Serializer.Deserialize<Contracts.Events.EventContext>(stream).ToClient();
    }

    [Fact] void should_carry_the_subject_across_the_wire() => _result.Subject.Value.ShouldEqual(TheSubject);
    [Fact] void should_keep_the_event_source_id_distinct_from_the_subject() => _result.EventSourceId.Value.ShouldEqual(TheEventSourceId);
}
