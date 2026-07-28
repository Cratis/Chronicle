// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.Jobs;
using Cratis.Chronicle.Projections;
using context = Cratis.Chronicle.Integration.Clustering.for_Clustering.when_replaying_a_collapsing_projection.context;

namespace Cratis.Chronicle.Integration.Clustering.for_Clustering;

/// <summary>
/// A replay drives one job step per partition, and those steps run in parallel across the silos of the cluster.
/// For a projection whose key collapses every partition onto one read model document, the read-modify-write cycle
/// that accumulates that document is serialized by a process-local lock - which only holds if every partition
/// reaches the same silo.
/// </summary>
/// <param name="_context">The <see cref="context"/> for the specification.</param>
[Collection(ChronicleCollection.Name)]
public class when_replaying_a_collapsing_projection(context _context)
    : IClassFixture<context>
{
    public class context(ClusteringFixture fixture) : IAsyncLifetime
    {
        const int Partitions = 24;
        const int EventsPerPartition = 5;
        const string Group = "collapsing-group";

        readonly TimeSpan _timeout = TimeSpan.FromSeconds(120);

        public static int ExpectedCount => Partitions * EventsPerPartition;

        public int CountAfterCatchUp { get; private set; }
        public int CountAfterReplay { get; private set; }
        public int SubscriberActivations { get; private set; }
        public int SilosHostingSubscriber { get; private set; }

        public async Task InitializeAsync()
        {
            var eventStore = fixture.ClientEventStore;
            var handler = eventStore.Projections.GetHandlerFor<CollapsingProjection>();
            await handler.WaitTillActive(_timeout);

            var lastSequenceNumber = EventSequenceNumber.Unavailable;
            for (var round = 0; round < EventsPerPartition; round++)
            {
                for (var partition = 0; partition < Partitions; partition++)
                {
                    var appendResult = await eventStore.EventLog.Append($"collapsing-source-{partition}", new CollapsedEvent(Group));
                    lastSequenceNumber = appendResult.SequenceNumber;
                }
            }

            await handler.WaitTillReachesEventSequenceNumber(lastSequenceNumber, _timeout);
            CountAfterCatchUp = await GetCount(eventStore);

            var replayJobId = await eventStore.Projections.Replay<CollapsingProjection>();
            await eventStore.Jobs.WaitTillJobCompletesOrIsDeleted(replayJobId, _timeout);
            await handler.WaitTillReachesEventSequenceNumber(lastSequenceNumber, _timeout);
            CountAfterReplay = await WaitForCount(eventStore, ExpectedCount);

            var subscriberActivations = await GetSubscriberActivations(handler.Id);
            SubscriberActivations = subscriberActivations.Length;
            SilosHostingSubscriber = subscriberActivations.Select(_ => _.SiloAddress).Distinct().Count();
        }

        public Task DisposeAsync() => Task.CompletedTask;

        static async Task<int> GetCount(IEventStore eventStore)
        {
            var readModel = await eventStore.ReadModels.GetInstanceById<CollapsingProjectionReadModel>(Group);
            return readModel?.Count ?? 0;
        }

        async Task<int> WaitForCount(IEventStore eventStore, int expected)
        {
            using var cancellationTokenSource = new CancellationTokenSource(_timeout);
            var count = 0;
            while (!cancellationTokenSource.IsCancellationRequested)
            {
                count = await GetCount(eventStore);
                if (count >= expected)
                {
                    return count;
                }

                await Task.Delay(250, cancellationTokenSource.Token).ConfigureAwait(ConfigureAwaitOptions.SuppressThrowing);
            }

            return count;
        }

        async Task<DetailedGrainStatistic[]> GetSubscriberActivations(ProjectionId projectionId)
        {
            var management = fixture.SiloServices.GetRequiredService<IGrainFactory>().GetGrain<IManagementGrain>(0);
            var statistics = await management.GetDetailedGrainStatistics();
            return [.. statistics.Where(statistic =>
                statistic.GrainType.Contains("projectionobserversubscriber", StringComparison.OrdinalIgnoreCase) &&
                statistic.GrainId.ToString().Contains(projectionId.Value, StringComparison.Ordinal))];
        }
    }

    [Fact] void should_accumulate_every_event_while_catching_up() => _context.CountAfterCatchUp.ShouldEqual(context.ExpectedCount);
    [Fact] void should_accumulate_every_event_while_replaying() => _context.CountAfterReplay.ShouldEqual(context.ExpectedCount);
    [Fact] void should_have_a_single_subscriber_activation_for_the_projection() => _context.SubscriberActivations.ShouldEqual(1);
    [Fact] void should_keep_the_subscriber_on_one_silo() => _context.SilosHostingSubscriber.ShouldEqual(1);
}
