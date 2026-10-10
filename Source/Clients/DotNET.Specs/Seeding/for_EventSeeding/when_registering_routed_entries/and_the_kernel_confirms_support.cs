// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Seeding.for_EventSeeding.when_registering_routed_entries;

public class and_the_kernel_confirms_support : given.a_seeding_builder
{
    async Task Because()
    {
        _seeding.ForNamespace("tenant").ForEventSource("source", "Lines", "line-1", [new TestEvent("value")], "Order");
        await _seeding.Register();
    }

    [Fact] void should_only_register_in_the_target_namespace() => _request.NamespacedEntries.Single().Namespace.ShouldEqual("tenant");
    [Fact] void should_not_register_global_entries() => _request.GlobalByEventSource.ShouldBeEmpty();
    [Fact] void should_route_by_source() => _request.NamespacedEntries.Single().ByEventSource.Single().Entries.Single().EventStreamId.ShouldEqual("line-1");
    [Fact] void should_route_by_type() => _request.NamespacedEntries.Single().ByEventType.Single().Entries.Single().EventStreamId.ShouldEqual("line-1");
}
