// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Events;

namespace Cratis.Chronicle.Server.Contracts.for_EventToAppend;

public class when_round_tripping_an_event_with_named_tags : Specification
{
    EventContext _legacyContext;
    Cratis.Chronicle.Contracts.Sequences.EventToAppendWithNamedTags _event;
    Cratis.Chronicle.Contracts.Sequences.AppendWithNamedTagsRequest _request;
    Cratis.Chronicle.Contracts.Sequences.EventContext _context;

    void Because()
    {
        _legacyContext = RoundTrip(new EventContext
        {
            NamedTags = new List<Cratis.Chronicle.Contracts.Events.NamedTag>
            {
                new() { Name = "account", Value = "one" }
            }
        });
        _event = RoundTrip(new Cratis.Chronicle.Contracts.Sequences.EventToAppendWithNamedTags
        {
            NamedTags = new List<Cratis.Chronicle.Contracts.Sequences.NamedTag>
            {
                new() { Name = "account", Value = "two" }
            }
        });
        _request = RoundTrip(new Cratis.Chronicle.Contracts.Sequences.AppendWithNamedTagsRequest
        {
            NamedTags = new List<Cratis.Chronicle.Contracts.Sequences.NamedTag>
            {
                new() { Name = "account", Value = "single" }
            }
        });
        _context = RoundTrip(new Cratis.Chronicle.Contracts.Sequences.EventContext
        {
            NamedTags = new List<Cratis.Chronicle.Contracts.Sequences.NamedTag>
            {
                new() { Name = "account", Value = "three" }
            }
        });
    }

    [Fact] void should_round_trip_legacy_context() => _legacyContext.NamedTags.Single().Value.ShouldEqual("one");
    [Fact] void should_round_trip_named_append_event() => _event.NamedTags.Single().Value.ShouldEqual("two");
    [Fact] void should_round_trip_generated_context() => _context.NamedTags.Single().Value.ShouldEqual("three");
    [Fact] void should_round_trip_the_single_append_request() => _request.NamedTags.Single().Value.ShouldEqual("single");

    static T RoundTrip<T>(T value)
        where T : class
    {
        using var stream = new MemoryStream();
        ProtoBuf.Serializer.Serialize(stream, value);
        stream.Position = 0;
        return ProtoBuf.Serializer.Deserialize<T>(stream);
    }
}
