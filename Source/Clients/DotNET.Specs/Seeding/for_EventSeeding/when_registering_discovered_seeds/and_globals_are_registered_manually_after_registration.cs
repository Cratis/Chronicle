// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Seeding.for_EventSeeding.when_registering_discovered_seeds;

public class and_globals_are_registered_manually_after_registration : given.a_seeding_builder_with_discovered_globals
{
    async Task Establish()
    {
        await _seeding.Discover();
        await _seeding.RegisterDiscovered();
        _requests.Clear();
        _seeding.For("manual", [new TestEvent("manual")]);
    }

    async Task Because()
    {
        await _seeding.Register();
        await _seeding.Register();
    }

    [Fact] void should_send_the_manual_globals_only_once() => _requests.SelectMany(_ => _.GlobalByEventSource).Select(_ => _.EventSourceId).ShouldContainOnly(["manual"]);
    [Fact] void should_clear_the_pending_buffer() => _requests.Count.ShouldEqual(1);
}
