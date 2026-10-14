// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Seeding.for_EventSeeding.given;

public class an_incomplete_discovery : a_seeding_builder_with_discovered_globals
{
    protected void FailActivation()
    {
        _artifacts.EventSeeders.Returns([.. _artifacts.EventSeeders, typeof(UnavailableSeeds)]);
        _activator.Activate<ICanSeedEvents>(Arg.Any<IServiceProvider>(), typeof(UnavailableSeeds))
            .Returns(_ => new Exception("Seeder unavailable"));
    }

    protected class UnavailableSeeds : ICanSeedEvents
    {
        public void Seed(IEventSeedingBuilder builder) => builder.For("unavailable", [new TestEvent("global")]);
    }
}
