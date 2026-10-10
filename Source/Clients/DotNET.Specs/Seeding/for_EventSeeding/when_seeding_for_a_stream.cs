// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Seeding.for_EventSeeding;

public class when_seeding_for_a_stream : given.a_seeding_builder
{
    async Task Because()
    {
        _seeding.For("source", "Lines", "line-1", [new TestEvent("value")], "Order");
        await _seeding.Register();
    }

    [Fact] void should_register_source_type() => _request.GlobalByEventSource.Single().Entries.Single().EventSourceType.ShouldEqual("Order");
    [Fact] void should_register_stream_type() => _request.GlobalByEventSource.Single().Entries.Single().EventStreamType.ShouldEqual("Lines");
    [Fact] void should_register_stream_id() => _request.GlobalByEventSource.Single().Entries.Single().EventStreamId.ShouldEqual("line-1");
    [Fact] void should_keep_static_tags() => _request.GlobalByEventSource.Single().Entries.Single().Tags.ShouldContain("static-tag");
}
