// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Integration.Clustering.for_ManyClientObservers;
using context = Cratis.Chronicle.Integration.Clustering.for_MultiplexedObservation.when_many_reactors_observe_over_one_stream.context;

namespace Cratis.Chronicle.Integration.Clustering.for_MultiplexedObservation;

/// <summary>
/// The same scenario that pins per-observer delivery, run with every reactor and reducer of a client sharing one stream.
/// </summary>
/// <param name="_context">The context.</param>
[Collection(MultiplexedChronicleCollection.Name)]
public class when_many_reactors_observe_over_one_stream(context _context)
    : IClassFixture<context>
{
    public class context(MultiplexedClusteringFixture fixture) : IAsyncLifetime
    {
        public ManyObserversScenario Scenario { get; } = new();

        public IReadOnlyList<RecordedMultiplexedObservation> Recordings { get; private set; } = [];

        public async ValueTask InitializeAsync()
        {
            await Scenario.Run(fixture.ClientEventStore, "multiplexed");
            Recordings = fixture.Recordings;
        }

        public ValueTask DisposeAsync() => ValueTask.CompletedTask;
    }

    [Fact] void should_deliver_to_every_reactor() => _context.Scenario.Received.Count.ShouldEqual(ManyObserversScenario.Reactors);
    [Fact] void should_deliver_every_event_to_every_reactor() => _context.Scenario.Received.Values.All(_ => _.Length == ManyObserversScenario.Events).ShouldBeTrue();
    [Fact] void should_deliver_in_the_order_appended() => _context.Scenario.Received.Values.All(_ => _.SequenceEqual(_context.Scenario.Appended)).ShouldBeTrue();
    [Fact] void should_have_a_connection_for_each_client() => _context.Recordings.Count.ShouldEqual(2);
    [Fact] void should_use_one_call_to_the_kernel_for_each_client() => _context.Recordings.All(_ => _.Calls == 1).ShouldBeTrue();
    [Fact] void should_register_several_observers_in_one_write() => _context.Recordings.Max(_ => _.Writes.Max()).ShouldBeGreaterThan(1);
}
