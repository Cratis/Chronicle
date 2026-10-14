// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Seeding.for_EventSeeding.when_registering_discovered_seeds;

public class without_a_shared_registration : given.a_seeding_builder_with_discovered_globals
{
    EventSeeding _independentSeeding;

    async Task Establish()
    {
        _independentSeeding = (EventSeeding)_seeding.CreateEmpty();
        await _independentSeeding.Discover();
    }

    async Task Because()
    {
        await _independentSeeding.Register();
        await _independentSeeding.Register();
    }

    [Fact] void should_send_discovered_globals() => _requests.SelectMany(_ => _.GlobalByEventSource).Select(_ => _.EventSourceId).ShouldContainOnly(["discovered"]);
    [Fact] void should_send_discovered_globals_authoritatively() => _requests.Single(_ => _.GlobalByEventSource.Any()).GlobalSetIsAuthoritative.ShouldBeTrue();
    [Fact] void should_clear_the_registered_entries() => _requests.Count.ShouldEqual(2);
}
