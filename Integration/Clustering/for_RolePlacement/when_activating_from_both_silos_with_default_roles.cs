// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using context = Cratis.Chronicle.Integration.Clustering.for_RolePlacement.when_activating_from_both_silos_with_default_roles.context;

namespace Cratis.Chronicle.Integration.Clustering.for_RolePlacement;

[Collection(DefaultRolesCollection.Name)]
public class when_activating_from_both_silos_with_default_roles(context _context) : IClassFixture<context>
{
    public class context(DefaultRolesFixture fixture) : IAsyncLifetime
    {
        public IReadOnlyList<SiloAddress> EligibleSilos => [fixture.EventSequencesSiloAddress, fixture.ObserversSiloAddress];
        public IReadOnlyList<GrainPlacement> Placements { get; private set; } = [];

        public async Task InitializeAsync() => Placements = await PlacementScenario.ActivateFromBothSilos(fixture);

        public Task DisposeAsync() => Task.CompletedTask;
    }

    [Fact] void should_observe_two_fresh_placements() => _context.Placements.Count.ShouldEqual(2);
    [Fact] void should_trigger_activation_from_each_silo() => _context.Placements.Select(placement => placement.Caller).ShouldContainOnly(_context.EligibleSilos);
    [Fact] void should_activate_observers_on_an_eligible_silo() => _context.Placements.Select(placement => placement.Observer).ShouldEachConformTo(address => _context.EligibleSilos.Contains(address));
    [Fact] void should_activate_event_sequences_on_an_eligible_silo() => _context.Placements.Select(placement => placement.EventSequence).ShouldEachConformTo(address => _context.EligibleSilos.Contains(address));
}
