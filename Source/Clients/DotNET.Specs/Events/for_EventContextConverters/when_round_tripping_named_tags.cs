// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using ProtoBuf;

namespace Cratis.Chronicle.Events.for_EventContextConverters;

public class when_round_tripping_named_tags : Specification
{
    Contracts.Events.EventContext _wire;
    EventContext _result;

    void Because()
    {
        var context = EventContext.EmptyWithEventSourceId("source") with
        {
            NamedTags = [new("name", ""), new("name", "another")]
        };
        using var stream = new MemoryStream();
        Serializer.Serialize(stream, context.ToContract());
        stream.Position = 0;
        _wire = Serializer.Deserialize<Contracts.Events.EventContext>(stream);
        _result = _wire.ToClient();
    }

    [Fact] void should_use_the_events_contract_named_tags() => _wire.NamedTags.Select(_ => (_.Name, _.Value)).ShouldEqual([("name", ""), ("name", "another")]);
    [Fact] void should_preserve_the_names_and_exact_values() => _result.NamedTags.Select(_ => (_.Name.Value, _.Value)).ShouldEqual([("name", ""), ("name", "another")]);
}
