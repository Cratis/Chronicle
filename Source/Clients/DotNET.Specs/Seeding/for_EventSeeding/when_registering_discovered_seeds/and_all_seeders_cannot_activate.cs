// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Seeding.for_EventSeeding.when_registering_discovered_seeds;

public class and_all_seeders_cannot_activate : given.an_incomplete_discovery
{
    async Task Establish()
    {
        FailActivation();
        _artifacts.EventSeeders.Returns([typeof(UnavailableSeeds)]);
        await _seeding.Discover();
    }

    Task Because() => _seeding.RegisterDiscovered();

    [Fact] void should_not_clear_the_previous_global_declaration() => _requests.ShouldBeEmpty();
}
