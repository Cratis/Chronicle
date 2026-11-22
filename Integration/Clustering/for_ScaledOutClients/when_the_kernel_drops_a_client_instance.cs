// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

extern alias KernelConcepts;
extern alias KernelCore;

using Cratis.Chronicle.Reactors;
using KernelCore::Cratis.Chronicle.Clients;
using context = Cratis.Chronicle.Integration.Clustering.for_ScaledOutClients.when_the_kernel_drops_a_client_instance.context;
using KernelConnectionId = KernelConcepts::Cratis.Chronicle.Concepts.Clients.ConnectionId;

namespace Cratis.Chronicle.Integration.Clustering.for_ScaledOutClients;

/// <summary>
/// Proves that when the kernel's connected clients registry drops a client instance, the departure notifier
/// hands that instance's observers over to the surviving instance well before the observer watchdog would.
/// The observer is never unsubscribed directly: the only trigger is <see cref="IConnectedClients.OnClientDisconnected"/>.
/// The dropped instance is still alive, so its next keep-alive restores its registry entry and its reactor's
/// KeepSubscription legitimately subscribes it again; the second round may therefore be served by either instance.
/// What this proves is the time from drop to takeover and that no partition is lost across it.
/// </summary>
/// <param name="_context">The spec context.</param>
[Collection(ChronicleCollection.Name)]
public class when_the_kernel_drops_a_client_instance(context _context)
    : IClassFixture<context>
{
    public class context(ClusteringFixture fixture) : IAsyncLifetime
    {
        public const int Partitions = 10;

        /// <summary>
        /// The default observer watchdog interval in seconds (Observers.WatchdogInterval in the kernel configuration); the fixture does not override it.
        /// </summary>
        public const int WatchdogIntervalInSeconds = 60;

        /// <summary>
        /// The takeover bound, well below one watchdog interval so a watchdog tick cannot be what recovered the observer.
        /// </summary>
        public static readonly TimeSpan TakeoverBound = TimeSpan.FromSeconds(15);

        readonly TimeSpan _timeout = TimeSpan.FromSeconds(60);

        public IReadOnlyCollection<HandledPartition> SecondRound { get; private set; } = [];
        public int TargetCountAfterDrop { get; private set; }
        public TimeSpan TakeoverTime { get; private set; }

        public async ValueTask InitializeAsync()
        {
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
                appendResult = await eventStore.EventLog.Append($"takeover-partition-{partition}", new ReroutedWorkPerformed(1));
            }

            await reactorHandler.WaitTillReachesEventSequenceNumber(appendResult.SequenceNumber, _timeout);
            await signal.WaitForHandledCount(Partitions, _timeout);

            // The kernel drops the second instance's connection through the registry, the same entry point
            // used when its stream ends or its keep-alive goes stale. The observer is not touched directly.
            var connectedClients = fixture.SiloServices.GetRequiredService<IGrainFactory>().GetConnectedClients(fixture.ObserversSiloAddress);
            var dropped = DateTimeOffset.UtcNow;
            await connectedClients.OnClientDisconnected((KernelConnectionId)fixture.SecondClientConnectionId, "Dropped by the kernel in a spec");

            TargetCountAfterDrop = await WaitForTargetCount(observer, expected: 1, TakeoverBound);
            TakeoverTime = DateTimeOffset.UtcNow - dropped;

            signal.Reset();
            for (var partition = 0; partition < Partitions; partition++)
            {
                appendResult = await eventStore.EventLog.Append($"takeover-partition-{partition}", new ReroutedWorkPerformed(2));
            }

            await reactorHandler.WaitTillReachesEventSequenceNumber(appendResult.SequenceNumber, TakeoverBound);
            await signal.WaitForHandledCount(Partitions, TakeoverBound);
            SecondRound = signal.Handled;
        }

        public ValueTask DisposeAsync() => ValueTask.CompletedTask;

        static async Task<int> WaitForTargetCount(KernelCore::Cratis.Chronicle.Observation.IObserver observer, int expected, TimeSpan timeout)
        {
            using var cancellationTokenSource = new CancellationTokenSource(timeout);
            var count = int.MaxValue;
            while (!cancellationTokenSource.IsCancellationRequested)
            {
                count = (await observer.GetSubscription()).Targets.Count;
                if (count == expected)
                {
                    return count;
                }

                await Task.Delay(100, cancellationTokenSource.Token).ConfigureAwait(ConfigureAwaitOptions.SuppressThrowing);
            }

            return count;
        }
    }

    [Fact] void should_have_a_single_fan_out_target_left() => _context.TargetCountAfterDrop.ShouldEqual(1);
    [Fact] void should_take_over_before_the_watchdog_could_have_run() => _context.TakeoverTime.ShouldBeLessThan(context.TakeoverBound);
    [Fact] void should_handle_every_partition_after_the_drop() => _context.SecondRound.Select(handled => handled.Partition).Distinct().Count().ShouldEqual(context.Partitions);
}
