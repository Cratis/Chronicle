// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

extern alias KernelCore;

using Cratis.Chronicle.Jobs;
using Cratis.Chronicle.Projections;
using context = Cratis.Chronicle.Integration.Clustering.for_RolePlacement.when_replaying_a_kernel_projection_with_split_roles.context;
using IProjectionObserverSubscriber = KernelCore::Cratis.Chronicle.Projections.IProjectionObserverSubscriber;

namespace Cratis.Chronicle.Integration.Clustering.for_RolePlacement;

[Collection(ReplayPlacementCollection.Name)]
public class when_replaying_a_kernel_projection_with_split_roles(context _context) : IClassFixture<context>
{
    public class context(ReplayPlacementFixture fixture) : IAsyncLifetime
    {
        public const int Partitions = 20;
        readonly TimeSpan _timeout = TimeSpan.FromSeconds(120);

        public SiloAddress JobsSilo => fixture.EventSequencesSiloAddress;
        public SiloAddress LiveSilo => fixture.ObserversSiloAddress;
        public ReplayPlacementSignal Placement => fixture.Placement;
        public IReadOnlyList<ReplayPlacementModel> Replayed { get; private set; } = [];
        public SiloAddress LiveSubscriber { get; private set; } = SiloAddress.Zero;

        public async Task InitializeAsync()
        {
            // This also waits for both silos' role metadata before selecting any test placement.
            await PlacementScenario.ActivateFromBothSilos(fixture);
            var store = fixture.ClientEventStore;
            var handler = store.Projections.GetHandlerFor<ReplayPlacementProjection>();
            await handler.WaitTillActive(_timeout);
            var sources = Enumerable.Range(0, Partitions).Select(_ => Guid.NewGuid().ToString()).ToArray();
            foreach (var source in sources)
            {
                var append = await store.EventLog.Append(source, new ReplayPlaced(42));
                await handler.WaitTillReachesEventSequenceNumber(append.SequenceNumber, _timeout);
            }

            var grains = fixture.SiloServices.GetRequiredService<IGrainFactory>();
            var management = grains.GetGrain<IManagementGrain>(0);
            var warmSubscribers = await management.GetDetailedGrainStatistics();
            foreach (var statistic in warmSubscribers.Where(statistic => IsSubscriber(statistic, handler.Id)))
            {
                await grains.GetGrain<IProjectionObserverSubscriber>(statistic.GrainId).ReleaseAfterPartition();
            }

            Placement.JobsSilo = JobsSilo;
            Placement.Capturing = true;
            var replay = await store.Projections.Replay<ReplayPlacementProjection>();
            await store.Jobs.WaitTillJobCompletesOrIsDeleted(replay, _timeout);
            Placement.Capturing = false;
            Replayed = await Task.WhenAll(sources.Select(source => store.ReadModels.GetInstanceById<ReplayPlacementModel>(source)));

            // An untouched partition must still activate on the observer's silo during live delivery.
            var liveSource = Guid.NewGuid().ToString();
            var liveAppend = await store.EventLog.Append(liveSource, new ReplayPlaced(99));
            await handler.WaitTillReachesEventSequenceNumber(liveAppend.SequenceNumber, _timeout);
            var liveStatistics = await management.GetDetailedGrainStatistics();
            LiveSubscriber = liveStatistics.Single(statistic => IsSubscriber(statistic, handler.Id) &&
                statistic.GrainId.ToString().Contains(liveSource, StringComparison.Ordinal)).SiloAddress;
        }

        public Task DisposeAsync() => Task.CompletedTask;

        static bool IsSubscriber(DetailedGrainStatistic statistic, ProjectionId projection) =>
            statistic.GrainType.Contains("projectionobserversubscriber", StringComparison.OrdinalIgnoreCase) &&
            statistic.GrainId.ToString().Contains(projection.Value, StringComparison.Ordinal);
    }

    [Fact] void should_run_a_real_step_for_every_partition() => _context.Placement.Steps.Count.ShouldBeGreaterThanOrEqual(context.Partitions);
    [Fact] void should_force_every_step_onto_the_jobs_silo() => _context.Placement.Steps.Values.ShouldEachConformTo(address => address == _context.JobsSilo);
    [Fact] void should_observe_every_replay_subscriber_before_release() => _context.Placement.Subscribers.Count.ShouldBeGreaterThanOrEqual(context.Partitions);
    [Fact] void should_place_at_least_ninety_percent_of_replay_subscribers_next_to_their_steps() => ((double)_context.Placement.Subscribers.Values.Count(address => address == _context.JobsSilo) / _context.Placement.Subscribers.Count).ShouldBeGreaterThanOrEqual(0.9);
    [Fact] void should_rebuild_every_partition() => _context.Replayed.ShouldEachConformTo(model => model.Number == 42);
    [Fact] void should_keep_live_delivery_on_the_observer_silo() => _context.LiveSubscriber.ShouldEqual(_context.LiveSilo);
}
