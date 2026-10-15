// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Seeding.for_EventSeeding.when_registering_discovered_seeds;

public class with_no_seeders : given.a_seeding_builder_with_discovered_globals
{
    async Task Establish()
    {
        _artifacts.EventSeeders.Returns([]);
        await _seeding.Discover();
    }

    async Task Because()
    {
        await _seeding.RegisterDiscovered();
        var other = (EventSeeding)_seeding.CreateEmpty();
        other.UseGlobalSeedRegistration(_registration);
        await other.Discover();
        await other.RegisterDiscovered();
    }

    [Fact] void should_send_an_authoritative_declaration_once() => _requests.Count(_ => _.GlobalSetIsAuthoritative).ShouldEqual(1);
    [Fact] void should_send_an_empty_global_set() => _requests.Single().GlobalByEventSource.ShouldBeEmpty();
}
