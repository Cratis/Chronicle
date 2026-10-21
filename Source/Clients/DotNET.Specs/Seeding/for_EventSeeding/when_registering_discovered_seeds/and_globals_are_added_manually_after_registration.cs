// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Seeding.for_EventSeeding.when_registering_discovered_seeds;

public class and_globals_are_added_manually_after_registration : given.a_seeding_builder_with_discovered_globals
{
    async Task Establish()
    {
        await _seeding.Discover();
        await _seeding.RegisterDiscovered();
        _requests.Clear();
        _seeding.For("manual-for", [new TestEvent("manual")]);
        _seeding.ForEventSource("manual-source", [new TestEvent("manual")]);
        _seeding.ForEvents([new("manual-routed", new TestEvent("manual"))]);
    }

    async Task Because() => await _seeding.RegisterDiscovered();

    [Fact] void should_send_the_manual_global_entries() => _requests.SelectMany(_ => _.GlobalByEventSource).Select(_ => _.EventSourceId).ShouldContainOnly(["manual-for", "manual-source", "manual-routed"]);
    [Fact] void should_not_resend_the_discovered_globals() => _requests.SelectMany(_ => _.GlobalByEventSource).Select(_ => _.EventSourceId).ShouldNotContain("discovered");
}
