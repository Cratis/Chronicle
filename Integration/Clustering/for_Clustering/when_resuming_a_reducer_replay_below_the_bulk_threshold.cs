// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

extern alias KernelCore;
extern alias KernelConcepts;

using System.Dynamic;
using System.Globalization;
using Cratis.Chronicle.Reducers;
using Cratis.Orleans.Jobs;

using Kernel = KernelCore::Cratis.Chronicle;
using KernelDomain = KernelConcepts::Cratis.Chronicle.Concepts;
using context = Cratis.Chronicle.Integration.Clustering.for_Clustering.when_resuming_a_reducer_replay_below_the_bulk_threshold.context;

namespace Cratis.Chronicle.Integration.Clustering.for_Clustering;

[Collection(ChronicleCollection.Name)]
public class when_resuming_a_reducer_replay_below_the_bulk_threshold(context _context) : IClassFixture<context>
{
    public class context(ClusteringFixture fixture) : IAsyncLifetime
    {
        public long DurableDocumentsBeforeResume;
        public int TotalAfterResume;
        public int TotalAfterLiveAppend;
        public int TotalAfterRepeatedPublication;
        public Kernel.Observation.Reducers.ReplayPublication OldPublication;
        public bool TargetsDiffer;

        public async Task InitializeAsync()
        {
            var timeout = TimeSpan.FromSeconds(60);
            var eventStore = fixture.ClientEventStore;
            var handler = eventStore.Reducers.GetHandlerFor<InterruptedReplayReducer>();
            await handler.WaitTillActive(timeout);
            var first = await eventStore.EventLog.Append("interrupted-reducer", new InterruptedNumberAdded(10));
            var second = await eventStore.EventLog.Append("interrupted-reducer", new InterruptedNumberAdded(5));
            await handler.WaitTillReachesEventSequenceNumber(second.SequenceNumber, timeout);

            var key = new KernelDomain.Observation.ObserverKey(typeof(InterruptedReplayReducer).FullName!, Constants.EventStore, KernelDomain.EventStoreNamespaceName.Default, KernelDomain.EventSequences.EventSequenceId.Log);
            var storage = fixture.SiloServices.GetRequiredService<Storage.IStorage>();
            var store = storage.GetEventStore(key.EventStore);
            var ns = store.GetNamespace(key.Namespace);
            var definition = await store.Reducers.Get(new(typeof(InterruptedReplayReducer).FullName!));
            var events = ns.GetEventSequence(key.EventSequenceId);
            var firstEvent = await events.GetEventAt(first.SequenceNumber.Value);
            var secondEvent = await events.GetEventAt(second.SequenceNumber.Value);
            var coordinator = fixture.SiloServices.GetRequiredService<IGrainFactory>().GetGrain<Kernel.Observation.Reducers.IReducerReplay>(key);
            var job = JobId.New();
            var original = await coordinator.Begin(job);
            var oldPipeline = await fixture.SiloServices.GetRequiredService<Kernel.Observation.Reducers.IReducerPipelineFactory>()
                .CreateForReplay(key.EventStore, key.Namespace, definition, original);
            await oldPipeline.Reduce(new([firstEvent], "interrupted-reducer"), (_, initial) => Task.FromResult(Result(initial, 10, firstEvent.Context.SequenceNumber)));
            DurableDocumentsBeforeResume = (await oldPipeline.Sink.GetInstances()).TotalCount;

            var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            var inFlight = oldPipeline.Reduce(new([secondEvent], "interrupted-reducer"), async (_, initial) =>
            {
                entered.SetResult();
                await release.Task.WaitAsync(timeout, TimeProvider.System);
                return Result(initial, 5, secondEvent.Context.SequenceNumber);
            });
            await entered.Task.WaitAsync(timeout, TimeProvider.System);
            var resumed = original;
            try
            {
                // Resume is a fresh attempt on the other silo. An old client reply remains in flight;
                // neither its completion nor its finalization may touch the replacement target or live sink.
                resumed = await coordinator.Begin(JobId.New());
                TargetsDiffer = original.ReplayContainerName != resumed.ReplayContainerName;
                var newPipeline = await fixture.SecondSiloServices.GetRequiredService<Kernel.Observation.Reducers.IReducerPipelineFactory>()
                    .CreateForReplay(key.EventStore, key.Namespace, definition, resumed);
                await newPipeline.Reduce(new([firstEvent, secondEvent], "interrupted-reducer"), (_, initial) => Task.FromResult(Result(initial, 15, secondEvent.Context.SequenceNumber)));
                release.SetResult();
                await inFlight;
                await coordinator.Abandon(job);
                OldPublication = await coordinator.Publish(original with { AllowEmptyResult = true });
                await coordinator.Publish(resumed with { AllowEmptyResult = true });
                TotalAfterResume = (await eventStore.ReadModels.GetInstanceById<InterruptedTotal>("interrupted-reducer")).Number;
            }
            finally
            {
                release.TrySetResult();
                await inFlight;
            }

            var appended = await eventStore.EventLog.Append("interrupted-reducer", new InterruptedNumberAdded(3));
            await handler.WaitTillReachesEventSequenceNumber(appended.SequenceNumber, timeout);
            TotalAfterLiveAppend = (await eventStore.ReadModels.GetInstanceById<InterruptedTotal>("interrupted-reducer")).Number;
            await coordinator.Publish(resumed with { AllowEmptyResult = true });
            TotalAfterRepeatedPublication = (await eventStore.ReadModels.GetInstanceById<InterruptedTotal>("interrupted-reducer")).Number;
        }

        public Task DisposeAsync() => Task.CompletedTask;

        static Kernel.Observation.Reducers.Clients.ReducerSubscriberResult Result(ExpandoObject? initial, int increment, KernelDomain.Events.EventSequenceNumber sequence)
        {
            var count = initial is null ? 0 : Convert.ToInt32(((IDictionary<string, object?>)initial)["number"], CultureInfo.InvariantCulture);
            var result = new ExpandoObject();
            ((IDictionary<string, object?>)result)["number"] = count + increment;
            return new(Kernel.Observation.ObserverSubscriberResult.Ok(sequence), result);
        }
    }

    [Fact] void should_persist_an_acknowledged_batch_before_final_flush() => _context.DurableDocumentsBeforeResume.ShouldEqual(1);
    [Fact] void should_restart_into_an_isolated_target() => _context.TargetsDiffer.ShouldBeTrue();
    [Fact] void should_fence_the_in_flight_superseded_attempt() => _context.OldPublication.ShouldEqual(Kernel.Observation.Reducers.ReplayPublication.Superseded);
    [Fact] void should_rebuild_instead_of_double_folding_acknowledged_state() => _context.TotalAfterResume.ShouldEqual(15);
    [Fact] void should_keep_both_silos_live_sinks_on_the_published_model() => _context.TotalAfterLiveAppend.ShouldEqual(18);
    [Fact] void should_preserve_live_writes_after_a_repeated_publication_rpc() => _context.TotalAfterRepeatedPublication.ShouldEqual(18);
}
