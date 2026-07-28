// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

extern alias KernelConcepts;

using Cratis.Chronicle.Reactors;
using context = Cratis.Chronicle.Integration.Clustering.for_ScaledOutClients.when_client_instance_disconnects.context;
using KernelConnectionId = KernelConcepts::Cratis.Chronicle.Concepts.Clients.ConnectionId;

namespace Cratis.Chronicle.Integration.Clustering.for_ScaledOutClients;

[Collection(ChronicleCollection.Name)]
public class when_client_instance_disconnects(context _context)
    : IClassFixture<context>
{
    public class context(ClusteringFixture fixture) : IAsyncLifetime
    {
        public const int Partitions = 10;
        readonly TimeSpan _timeout = TimeSpan.FromSeconds(60);

        public IReadOnlyCollection<HandledPartition> SecondRound { get; private set; } = [];
        public int TargetCountAfterDisconnect { get; private set; }
        public string RemainingInstance { get; private set; } = string.Empty;

        public async Task InitializeAsync()
        {
            RemainingInstance = fixture.EventSequencesSiloAddress.ToParsableString();

            var signal = fixture.FanOutSignal;
            signal.Reset();

            var eventStore = fixture.ClientEventStore;
            _ = fixture.SecondClientEventStore;

            var reactorHandler = eventStore.Reactors.GetHandlerFor<FanOutReactor>();
            await reactorHandler.WaitTillActive(_timeout);

            var observer = fixture.GetObserverFor<FanOutReactor>();
            await fixture.WaitForFanOutTargets(observer, expected: 2, _timeout);

            // Round 1 establishes the two-instance distribution.
            var appendResult = default(EventSequences.AppendResult)!;
            for (var partition = 0; partition < Partitions; partition++)
            {
                appendResult = await eventStore.EventLog.Append($"rerouted-partition-{partition}", new ReroutedWorkPerformed(1));
            }

            await reactorHandler.WaitTillReachesEventSequenceNumber(appendResult.SequenceNumber, _timeout);
            await signal.WaitForHandledCount(Partitions, _timeout);

            // Simulate the second client instance's stream ending - this is exactly what the
            // kernel's reactor service does when a client's observe stream is cancelled.
            await observer.UnsubscribeIfMatchesClient((KernelConnectionId)fixture.SecondClientConnectionId);
            TargetCountAfterDisconnect = (await observer.GetSubscription()).Targets.Count;

            // Round 2: every partition - including those previously mapped to the removed
            // instance - must now be handled by the remaining instance.
            signal.Reset();
            for (var partition = 0; partition < Partitions; partition++)
            {
                appendResult = await eventStore.EventLog.Append($"rerouted-partition-{partition}", new ReroutedWorkPerformed(2));
            }

            await reactorHandler.WaitTillReachesEventSequenceNumber(appendResult.SequenceNumber, _timeout);
            await signal.WaitForHandledCount(Partitions, _timeout);
            SecondRound = signal.Handled;
        }

        public Task DisposeAsync() => Task.CompletedTask;
    }

    [Fact] void should_have_a_single_fan_out_target_left() => _context.TargetCountAfterDisconnect.ShouldEqual(1);
    [Fact] void should_handle_every_partition_in_the_second_round() => _context.SecondRound.Select(handled => handled.Partition).Distinct().Count().ShouldEqual(context.Partitions);
    [Fact] void should_route_every_partition_to_the_remaining_instance() => _context.SecondRound.Select(handled => handled.Instance).Distinct().ShouldContainOnly(_context.RemainingInstance);
}
