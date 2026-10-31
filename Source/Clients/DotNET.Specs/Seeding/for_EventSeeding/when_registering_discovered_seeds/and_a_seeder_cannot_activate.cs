// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Seeding.for_EventSeeding.when_registering_discovered_seeds;

public class and_a_seeder_cannot_activate : given.an_incomplete_discovery
{
    async Task Establish()
    {
        FailActivation();
        _seeding.For("manual", [new TestEvent("manual")]);
        await _seeding.Discover();
    }

    Task Because() => _seeding.RegisterDiscovered();

    [Fact] void should_not_send_an_authoritative_declaration() => _requests.Exists(_ => _.GlobalSetIsAuthoritative).ShouldBeFalse();
    [Fact] void should_not_make_partial_discovered_globals_permanent() => _requests.SelectMany(_ => _.GlobalByEventSource).Select(_ => _.EventSourceId).ShouldContainOnly(["manual"]);
    [Fact] void should_send_the_namespaced_entries() => _requests.SelectMany(_ => _.NamespacedEntries).Select(_ => _.Namespace).ShouldContainOnly(["tenant"]);
}
