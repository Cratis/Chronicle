// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Seeding.for_EventSeeding.when_registering_discovered_seeds;

public class and_two_namespace_stores_share_the_registration : given.a_seeding_builder_with_discovered_globals
{
    EventSeeding _otherSeeding;

    async Task Establish()
    {
        _otherSeeding = (EventSeeding)_seeding.CreateEmpty();
        _otherSeeding.UseGlobalSeedRegistration(_registration);
        await _seeding.Discover();
        await _otherSeeding.Discover();
        await _seeding.RegisterDiscovered();
    }

    async Task Because() => await _otherSeeding.RegisterDiscovered();

    [Fact] void should_send_the_discovered_globals_only_once() => _requests.SelectMany(_ => _.GlobalByEventSource).Select(_ => _.EventSourceId).ShouldContainOnly(["discovered"]);
    [Fact] void should_declare_the_global_set_once() => _requests.Count(_ => _.GlobalSetIsAuthoritative).ShouldEqual(1);
    [Fact] void should_send_namespaced_entries_from_both_stores() => _requests.Count(_ => _.NamespacedEntries.Any()).ShouldEqual(2);
}
