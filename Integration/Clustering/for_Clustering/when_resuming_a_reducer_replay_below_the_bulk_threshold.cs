// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

extern alias KernelCore;
extern alias KernelConcepts;

using Cratis.Chronicle.Reducers;
using context = Cratis.Chronicle.Integration.Clustering.for_Clustering.when_resuming_a_reducer_replay_below_the_bulk_threshold.context;

namespace Cratis.Chronicle.Integration.Clustering.for_Clustering;

[Collection(ChronicleCollection.Name)]
public class when_resuming_a_reducer_replay_below_the_bulk_threshold(context _context) : IClassFixture<context>
{
    public class context(ClusteringFixture fixture) : IAsyncLifetime
    {
        public long DurableDocumentsBeforeResume;
        public int TotalAfterResume;

        public async Task InitializeAsync()
        {
            var timeout = TimeSpan.FromSeconds(60);
            var eventStore = fixture.ClientEventStore;
            var handler = eventStore.Reducers.GetHandlerFor<InterruptedReplayReducer>();
            await handler.WaitTillActive(timeout);
            var services = fixture.SiloServices;
            var replay = services.GetRequiredService<KernelCore::Cratis.Chronicle.Observation.IObserverServiceClient>();
            var details = new KernelCore::Cratis.Chronicle.Observation.ObserverDetails(
                new(typeof(InterruptedReplayReducer).FullName!, Constants.EventStore, KernelConcepts::Cratis.Chronicle.Concepts.EventStoreNamespaceName.Default, KernelConcepts::Cratis.Chronicle.Concepts.EventSequences.EventSequenceId.Log),
                KernelConcepts::Cratis.Chronicle.Concepts.Observation.ObserverType.Reducer);
            await replay.BeginReplayFor(details);
            try
            {
                // Drive one acknowledged reducer batch into the replay sink. Far fewer than 1000 operations
                // must already be durable: ResumeReplay resets MongoDB's pending bulk operations and cache.
                var append = await eventStore.EventLog.Append("interrupted-reducer", new InterruptedNumberAdded(10));
                await handler.WaitTillReachesEventSequenceNumber(append.SequenceNumber, timeout);
                var storage = services.GetRequiredService<Storage.IStorage>();
                var store = storage.GetEventStore(details.Key.EventStore);
                var definition = await store.Reducers.Get(new(typeof(InterruptedReplayReducer).FullName!));
                var readModel = await store.ReadModels.Get(definition.ReadModel);
                var sink = await store.GetNamespace(details.Key.Namespace).Sinks.GetFor(readModel);
                DurableDocumentsBeforeResume = (await sink.GetInstances($"replay-{readModel.ContainerName}")).TotalCount;

                // Model a restart/resume after the batch has been acknowledged. No EndReplay/EndBulk is called
                // by the test between that acknowledgment and ResumeReplay.
                await replay.ResumeReplayFor(details);
                append = await eventStore.EventLog.Append("interrupted-reducer", new InterruptedNumberAdded(5));
                await handler.WaitTillReachesEventSequenceNumber(append.SequenceNumber, timeout);
                await replay.EndReplayFor(details with { ReplaySucceededWithEvents = true });
                TotalAfterResume = (await eventStore.ReadModels.GetInstanceById<InterruptedTotal>("interrupted-reducer")).Number;
            }
            catch
            {
                await replay.EndReplayFor(details with { ReplayAborted = true });
                throw;
            }
        }

        public Task DisposeAsync() => Task.CompletedTask;
    }

    [Fact] void should_persist_an_acknowledged_batch_before_final_flush() => _context.DurableDocumentsBeforeResume.ShouldEqual(1);
    [Fact] void should_keep_acknowledged_state_when_resuming() => _context.TotalAfterResume.ShouldEqual(15);
}
