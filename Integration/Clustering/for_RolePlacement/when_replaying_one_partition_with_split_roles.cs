// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

extern alias KernelCore;
extern alias KernelConcepts;

using Cratis.Chronicle.Projections;
using context = Cratis.Chronicle.Integration.Clustering.for_RolePlacement.when_replaying_one_partition_with_split_roles.context;
using EventSequenceId = KernelConcepts::Cratis.Chronicle.Concepts.EventSequences.EventSequenceId;
using IHandleEventsForPartition = KernelCore::Cratis.Chronicle.Observation.Jobs.IHandleEventsForPartition;
using IObserver = KernelCore::Cratis.Chronicle.Observation.IObserver;
using ObserverKey = KernelConcepts::Cratis.Chronicle.Concepts.Observation.ObserverKey;

namespace Cratis.Chronicle.Integration.Clustering.for_RolePlacement;

[Collection(ReplayPlacementCollection.Name)]
public class when_replaying_one_partition_with_split_roles(context _context) : IClassFixture<context>
{
    public class context(ReplayPlacementFixture fixture) : IAsyncLifetime
    {
        readonly TimeSpan _timeout = TimeSpan.FromSeconds(120);

        public ReplayPlacementSignal Placement => fixture.Placement;
        public SiloAddress JobsSilo => fixture.EventSequencesSiloAddress;
        public GrainId Release { get; private set; }
        public GrainId DeactivatedSubscriber { get; private set; }
        public ReplayPlacementModel? Replayed { get; private set; }
        public SiloAddress LiveSubscriber { get; private set; } = SiloAddress.Zero;
        public SiloAddress LiveSilo => fixture.ObserversSiloAddress;

        public async Task InitializeAsync()
        {
            await PlacementScenario.ActivateFromBothSilos(fixture);
            var store = fixture.ClientEventStore;
            var handler = store.Projections.GetHandlerFor<ReplayPlacementProjection>();
            await handler.WaitTillActive(_timeout);
            var source = Guid.NewGuid().ToString();
            var append = await store.EventLog.Append(source, new ReplayPlaced(42));
            await handler.WaitTillReachesEventSequenceNumber(append.SequenceNumber, _timeout);

            var grains = fixture.SiloServices.GetRequiredService<IGrainFactory>();
            var key = new ObserverKey(handler.Id.Value, store.Name.Value, store.Namespace.Value, EventSequenceId.Log);
            Placement.BeginCapture(JobsSilo, typeof(IHandleEventsForPartition), handler.Id.Value, source);
            await grains.GetGrain<IObserver>(key).ReplayPartition(source);
            Release = await Placement.Released.Task.WaitAsync(_timeout);
            DeactivatedSubscriber = await Placement.Deactivated.Task.WaitAsync(_timeout);
            await Placement.PartitionCompleted.Task.WaitAsync(_timeout);
            Placement.Capturing = false;
            Replayed = await store.ReadModels.GetInstanceById<ReplayPlacementModel>(source);

            // The real partition step released the formerly warm subscriber. The next live delivery must
            // reactivate the same identity next to its observer rather than keep it on the jobs silo.
            var liveAppend = await store.EventLog.Append(source, new ReplayPlaced(99));
            await handler.WaitTillReachesEventSequenceNumber(liveAppend.SequenceNumber, _timeout);
            var statistics = await grains.GetGrain<IManagementGrain>(0).GetDetailedGrainStatistics();
            LiveSubscriber = statistics.Single(statistic => statistic.GrainId == Release).SiloAddress;
        }

        public Task DisposeAsync() => Task.CompletedTask;
    }

    [Fact] void should_run_one_real_partition_step() => _context.Placement.Steps.Count.ShouldEqual(1);
    [Fact] void should_run_the_step_on_the_jobs_silo() => _context.Placement.Steps.Values.ShouldEachConformTo(address => address == _context.JobsSilo);
    [Fact] void should_release_after_the_real_partition_step() => _context.Release.ShouldNotEqual(default);
    [Fact] void should_deactivate_the_released_subscriber() => _context.DeactivatedSubscriber.ShouldEqual(_context.Release);
    [Fact] void should_replay_the_partition() => _context.Replayed!.Number.ShouldEqual(42);
    [Fact] void should_reactivate_the_same_subscriber_next_to_the_live_observer() => _context.LiveSubscriber.ShouldEqual(_context.LiveSilo);
}
