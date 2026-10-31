// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Seeding.for_EventSeeding.when_registering_discovered_seeds;

public class and_manual_globals_were_pending_before_discovery : given.a_seeding_builder_with_discovered_globals
{
    async Task Establish()
    {
        await _seeding.Discover();
        await _seeding.RegisterDiscovered();
        _requests.Clear();
        _seeding.For("manual", [new TestEvent("manual")]);
        await _seeding.Discover();
    }

    async Task Because() => await _seeding.RegisterDiscovered();

    [Fact] void should_send_the_pending_manual_globals() => _requests.SelectMany(_ => _.GlobalByEventSource).Select(_ => _.EventSourceId).ShouldContainOnly(["manual"]);
    [Fact] void should_send_the_discovered_namespaced_entries() => _requests.SelectMany(_ => _.NamespacedEntries).Select(_ => _.Namespace).ShouldContainOnly(["tenant"]);
}
