// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using context = Cratis.Chronicle.Integration.Clustering.for_ManyClientObservers.when_many_reactors_observe_one_event_source.context;

namespace Cratis.Chronicle.Integration.Clustering.for_ManyClientObservers;

/// <summary>
/// Pins that every reactor registered on one connection receives every event, in order, whatever the number of reactors.
/// </summary>
/// <param name="_context">The context.</param>
[Collection(ChronicleCollection.Name)]
public class when_many_reactors_observe_one_event_source(context _context)
    : IClassFixture<context>
{
    public class context(ClusteringFixture fixture) : IAsyncLifetime
    {
        public ManyObserversScenario Scenario { get; } = new();

        public async ValueTask InitializeAsync() => await Scenario.Run(fixture.ClientEventStore, "per-observer-stream");

        public ValueTask DisposeAsync() => ValueTask.CompletedTask;
    }

    [Fact] void should_deliver_to_every_reactor() => _context.Scenario.Received.Count.ShouldEqual(ManyObserversScenario.Reactors);
    [Fact] void should_deliver_every_event_to_every_reactor() => _context.Scenario.Received.Values.All(_ => _.Length == ManyObserversScenario.Events).ShouldBeTrue();
    [Fact] void should_deliver_in_the_order_appended() => _context.Scenario.Received.Values.All(_ => _.SequenceEqual(_context.Scenario.Appended)).ShouldBeTrue();
}
