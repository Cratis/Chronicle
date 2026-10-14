// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Seeding;
using Cratis.Chronicle.Namespaces;
using Cratis.Chronicle.Patterns;
using Microsoft.Extensions.Logging.Abstractions;

namespace Cratis.Chronicle.Seeding.for_EventSeeding;

public class when_a_namespace_is_added_after_replacement : given.an_owned_global_seed_set
{
    NamespacesReactor _reactor;
    IResultAwareEventSeeding _futureGrain;

    async Task Establish()
    {
        await _grain.SeedGlobalWithResult([AnEntry(5)], true, "program");
        var global = Substitute.For<IResultAwareEventSeeding>();
        global.GetSeededEvents().Returns(_ => Task.FromResult(_stored));
        _grainFactory.GetGrain<IResultAwareEventSeeding>(EventSeedingKey.ForGlobal("TestEventStore").ToString()).Returns(global);
        _futureGrain = Substitute.For<IResultAwareEventSeeding>();
        _futureGrain.SeedWithResult(Arg.Any<IEnumerable<SeedingEntry>>()).Returns(Task.FromResult(SeedingResult.Complete));
        _grainFactory.GetGrain<IResultAwareEventSeeding>(EventSeedingKey.ForNamespace("TestEventStore", "future").ToString()).Returns(_futureGrain);
        _reactor = new(_grainFactory, Substitute.For<IPatternCapture>(), NullLogger<NamespacesReactor>.Instance);
    }

    Task Because() => _reactor.Added(new NamespaceAdded("TestEventStore", "future"), null!);

    [Fact] void should_offer_only_the_remaining_global_set() => _futureGrain.Received(1).SeedWithResult(Arg.Is<IEnumerable<SeedingEntry>>(_ => _.Select(entry => entry.EventSourceId).OrderBy(id => id.Value).SequenceEqual(StoredSources.OrderBy(id => id.Value))));
}
