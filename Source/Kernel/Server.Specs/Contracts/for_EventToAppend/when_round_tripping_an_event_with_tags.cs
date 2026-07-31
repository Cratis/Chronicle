// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Events;

namespace Cratis.Chronicle.Server.Contracts.for_EventToAppend;

/// <summary>
/// protobuf-net deserializes a repeated field into the instance the property was initialized with, and refuses to
/// add to one that reports itself read-only. Defaulting the tags to a collection expression typed as
/// <see cref="IEnumerable{T}"/> produced an array, so appending any event carrying a tag failed on the server.
/// </summary>
public class when_round_tripping_an_event_with_tags : Specification
{
    EventToAppend _source;
    EventToAppend _result;

    void Establish() => _source = new()
    {
        EventSourceId = "some-event-source",
        EventType = new EventType { Id = "some-event-type", Generation = 1 },
        Content = "{}",
        Tags = new List<string> { "audit", "billing" },
    };

    void Because()
    {
        using var stream = new MemoryStream();
        ProtoBuf.Serializer.Serialize(stream, _source);
        stream.Position = 0;
        _result = ProtoBuf.Serializer.Deserialize<EventToAppend>(stream);
    }

    [Fact] void should_carry_the_tags_across() => _result.Tags.ShouldContainOnly("audit", "billing");
}
