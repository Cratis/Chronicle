// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Seeding.for_EventSeeding.when_registering_discovered_seeds;

public class and_reconnecting_with_no_globals : given.a_seeding_builder_with_discovered_globals
{
    async Task Establish()
    {
        _artifacts.EventSeeders.Returns([]);
        await _seeding.Discover();
        await _seeding.RegisterDiscovered();
        await _registration.Reset();
    }

    Task Because() => _seeding.RegisterDiscovered();

    [Fact] void should_declare_the_empty_set_again() => _requests.Count(_ => _.GlobalSetIsAuthoritative && !_.GlobalByEventSource.Any()).ShouldEqual(2);
}
