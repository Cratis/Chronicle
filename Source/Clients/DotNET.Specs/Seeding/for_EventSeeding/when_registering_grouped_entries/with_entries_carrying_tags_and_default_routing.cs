// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.Seeding.for_EventSeeding.when_registering_grouped_entries;

public class with_entries_carrying_tags_and_default_routing : given.a_seeding_builder_that_records_serialization
{
    void Establish()
    {
        _seeding.ForEventSource("s1", [new Alpha("a1"), new Beta("b1")]);
        _seeding.ForNamespace("ns").ForEventSource("s2", [new Alpha("n1")]);
    }

    async Task Because() => await _seeding.Register();

    [Fact] void should_carry_the_static_tags_of_the_event_type() => _requests[0].GlobalByEventSource.ElementAt(0).Entries[0].Tags.ShouldEqual<string>(["alpha-tag"]);
    [Fact] void should_have_no_tags_for_an_untagged_event_type() => _requests[0].GlobalByEventSource.ElementAt(0).Entries[1].Tags.ShouldBeEmpty();
    [Fact] void should_carry_the_static_tags_for_namespaced_entries() => _requests[0].NamespacedEntries.ElementAt(0).ByEventSource[0].Entries[0].Tags.ShouldEqual<string>(["alpha-tag"]);
    [Fact] void should_default_the_event_source_type() => _requests[0].GlobalByEventSource.ElementAt(0).Entries[0].EventSourceType.ShouldEqual(EventSourceType.Default.Value);
    [Fact] void should_default_the_event_stream_type() => _requests[0].GlobalByEventSource.ElementAt(0).Entries[0].EventStreamType.ShouldEqual(EventStreamType.All.Value);
    [Fact] void should_default_the_event_stream_id() => _requests[0].GlobalByEventSource.ElementAt(0).Entries[0].EventStreamId.ShouldEqual(EventStreamId.Default);
    [Fact] void should_not_ask_whether_routing_is_supported() => _seedingService.DidNotReceive().GetSeedingSupport(Arg.Any<ProtoBuf.Grpc.CallContext>());
}
