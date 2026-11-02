// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using context = Cratis.Chronicle.Integration.Clustering.for_RolePlacement.when_activating_from_both_silos_with_split_roles.context;

namespace Cratis.Chronicle.Integration.Clustering.for_RolePlacement;

[Collection(ChronicleCollection.Name)]
public class when_activating_from_both_silos_with_split_roles(context _context) : IClassFixture<context>
{
    public class context(ClusteringFixture fixture) : IAsyncLifetime
    {
        public SiloAddress EventSequencesSilo => fixture.EventSequencesSiloAddress;
        public SiloAddress ObserversSilo => fixture.ObserversSiloAddress;
        public IReadOnlyList<GrainPlacement> Placements { get; private set; } = [];

        public async Task InitializeAsync() => Placements = await PlacementScenario.ActivateFromBothSilos(fixture);

        public Task DisposeAsync() => Task.CompletedTask;
    }

    [Fact] void should_observe_two_fresh_placements() => _context.Placements.Count.ShouldEqual(2);
    [Fact] void should_trigger_activation_from_each_silo() => _context.Placements.Select(placement => placement.Caller).ShouldContainOnly(_context.EventSequencesSilo, _context.ObserversSilo);
    [Fact] void should_place_observers_only_on_the_observers_silo() => _context.Placements.Select(placement => placement.Observer).ShouldEachConformTo(address => address == _context.ObserversSilo);
    [Fact] void should_place_event_sequences_only_on_the_event_sequences_silo() => _context.Placements.Select(placement => placement.EventSequence).ShouldEachConformTo(address => address == _context.EventSequencesSilo);
}
