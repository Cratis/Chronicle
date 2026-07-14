// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts;
using Cratis.Chronicle.Reactors;
using context = Cratis.Chronicle.Integration.Clustering.for_ScaledOutClients.when_two_client_instances_are_connected.context;
using ContractsConnectedClient = Cratis.Chronicle.Contracts.Clients.ConnectedClient;

namespace Cratis.Chronicle.Integration.Clustering.for_ScaledOutClients;

[Collection(ChronicleCollection.Name)]
public class when_two_client_instances_are_connected(context _context)
    : IClassFixture<context>
{
    public class context(ClusteringFixture fixture) : IAsyncLifetime
    {
        public const int Partitions = 20;
        readonly TimeSpan _timeout = TimeSpan.FromSeconds(60);

        public IReadOnlyCollection<HandledPartition> FirstRound { get; private set; } = [];
        public IReadOnlyCollection<HandledPartition> SecondRound { get; private set; } = [];
        public IEnumerable<ContractsConnectedClient> ConnectedClients { get; private set; } = [];
        public int TargetCount { get; private set; }
        public string FirstInstance { get; private set; } = string.Empty;
        public string SecondInstance { get; private set; } = string.Empty;

        public async Task InitializeAsync()
        {
            FirstInstance = fixture.EventSequencesSiloAddress.ToParsableString();
            SecondInstance = fixture.ObserversSiloAddress.ToParsableString();

            var signal = fixture.ScaledOutSignal;
            signal.Reset();

            var eventStore = fixture.ClientEventStore;
            _ = fixture.SecondClientEventStore;

            var reactorHandler = eventStore.Reactors.GetHandlerFor<ScaledOutReactor>();
            await reactorHandler.WaitTillActive(_timeout);

            var observer = fixture.GetObserverFor<ScaledOutReactor>();
            await fixture.WaitForFanOutTargets(observer, expected: 2, _timeout);

            // Round 1: one event per partition - every partition must be handled exactly once,
            // spread across both client instances.
            var appendResult = default(EventSequences.AppendResult)!;
            for (var partition = 0; partition < Partitions; partition++)
            {
                appendResult = await eventStore.EventLog.Append($"scaled-partition-{partition}", new ScaledWorkPerformed(1));
            }

            await reactorHandler.WaitTillReachesEventSequenceNumber(appendResult.SequenceNumber, _timeout);
            await signal.WaitForHandledCount(Partitions, _timeout);
            FirstRound = signal.Handled;

            // Round 2: same partitions again - each partition must land on the same instance as in
            // round 1, proving the round-robin fan out is sticky per partition key.
            signal.Reset();
            for (var partition = 0; partition < Partitions; partition++)
            {
                appendResult = await eventStore.EventLog.Append($"scaled-partition-{partition}", new ScaledWorkPerformed(2));
            }

            await reactorHandler.WaitTillReachesEventSequenceNumber(appendResult.SequenceNumber, _timeout);
            await signal.WaitForHandledCount(Partitions, _timeout);
            SecondRound = signal.Handled;

            var subscription = await observer.GetSubscription();
            TargetCount = subscription.Targets.Count;

            ConnectedClients = await fixture.SiloServices.GetRequiredService<IServices>().Connections.GetConnectedClients();
        }

        public Task DisposeAsync() => Task.CompletedTask;
    }

    [Fact] void should_have_two_connected_clients() => _context.ConnectedClients.Count().ShouldEqual(2);
    [Fact] void should_have_a_client_connected_to_each_silo() => _context.ConnectedClients.Select(client => client.SiloAddress).ShouldContainOnly(_context.FirstInstance, _context.SecondInstance);
    [Fact] void should_have_two_fan_out_targets() => _context.TargetCount.ShouldEqual(2);
    [Fact] void should_handle_every_partition() => _context.FirstRound.Select(handled => handled.Partition).Distinct().Count().ShouldEqual(context.Partitions);
    [Fact] void should_handle_every_partition_exactly_once() => _context.FirstRound.Count.ShouldEqual(context.Partitions);
    [Fact] void should_spread_partitions_across_both_instances() => _context.FirstRound.Select(handled => handled.Instance).Distinct().Count().ShouldEqual(2);
    [Fact]
    void should_keep_partitions_sticky_to_their_instance()
    {
        var firstRoundByPartition = _context.FirstRound.ToDictionary(handled => handled.Partition, handled => handled.Instance);
        foreach (var handled in _context.SecondRound)
        {
            handled.Instance.ShouldEqual(firstRoundByPartition[handled.Partition]);
        }
    }
}
