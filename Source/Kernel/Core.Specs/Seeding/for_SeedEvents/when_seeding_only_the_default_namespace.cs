// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Seeding;

namespace Cratis.Chronicle.Seeding.for_SeedEvents;

public class when_seeding_only_the_default_namespace : Specification
{
    IGrainFactory _grainFactory;
    IResultAwareEventSeeding _defaultSeeds;
    SeedEvents _command;

    void Establish()
    {
        _grainFactory = Substitute.For<IGrainFactory>();
        _defaultSeeds = Substitute.For<IResultAwareEventSeeding>();
        _defaultSeeds.SeedWithResult(Arg.Any<IEnumerable<SeedingEntry>>()).Returns(SeedingResult.Complete);
        _grainFactory.GetGrain<IResultAwareEventSeeding>(EventSeedingKey.ForNamespace("store", EventStoreNamespaceName.Default).ToString()).Returns(_defaultSeeds);
        var namespaced = new Contracts.Seeding.NamespacedSeedEntries
        {
            Namespace = EventStoreNamespaceName.Default,
            ByEventSource = [new Contracts.Seeding.EventSourceSeedEntries
            {
                EventSourceId = "local",
                Entries = [new Contracts.Seeding.SeedingEntry { EventSourceId = "local", EventTypeId = "event", Content = "{}" }]
            }]
        };
        _command = new("store", [], [], [namespaced]);
    }

    async Task Because() => await _command.Handle(_grainFactory);

    [Fact] void should_seed_the_default_namespace() => _defaultSeeds.Received(1).SeedWithResult(Arg.Is<IEnumerable<SeedingEntry>>(_ => _.Single().EventSourceId == "local"));
    [Fact] void should_not_resolve_any_other_namespace_or_the_global_grain() => _grainFactory.Received(1).GetGrain<IResultAwareEventSeeding>(Arg.Any<string>(), Arg.Any<string>());
}
