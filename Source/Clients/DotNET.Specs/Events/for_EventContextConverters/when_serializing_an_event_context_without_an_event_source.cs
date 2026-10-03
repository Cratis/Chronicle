// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using ProtoBuf;

namespace Cratis.Chronicle.Events.for_EventContextConverters;

public class when_serializing_an_event_context_without_an_event_source : Specification
{
    Contracts.Events.EventContext _result;

    void Because()
    {
        using var stream = new MemoryStream();
        Serializer.Serialize(stream, EventContext.Empty.ToContract());
        stream.Position = 0;
        _result = Serializer.Deserialize<Contracts.Events.EventContext>(stream);
    }

    [Fact] void should_serialize_not_set_as_an_empty_wire_value() => _result.EventSource.ShouldEqual(string.Empty);
}
