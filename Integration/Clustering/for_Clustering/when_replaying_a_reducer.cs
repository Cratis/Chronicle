// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.Jobs;
using Cratis.Chronicle.Reducers;
using context = Cratis.Chronicle.Integration.Clustering.for_Clustering.when_replaying_a_reducer.context;

namespace Cratis.Chronicle.Integration.Clustering.for_Clustering;

[Collection(ChronicleCollection.Name)]
public class when_replaying_a_reducer(context _context) : IClassFixture<context>
{
    public class context(ClusteringFixture fixture) : IAsyncLifetime
    {
        const int Partitions = 24;
        readonly TimeSpan _timeout = TimeSpan.FromSeconds(60);
        public int[] BeforeReplay;
        public int[] AfterReplay;
        public int[] AfterEmptyReplay;
        public int AfterLiveAppend;

        public async Task InitializeAsync()
        {
            var eventStore = fixture.ClientEventStore;
            var handler = eventStore.Reducers.GetHandlerFor<ClusteredReplayReducer>();
            await handler.WaitTillActive(_timeout);
            await fixture.SecondClientEventStore.Reducers.GetHandlerFor<ClusteredReplayReducer>().WaitTillActive(_timeout);
            fixture.ReplayCalculation.Multiplier = 1;
            var last = EventSequenceNumber.Unavailable;
            for (var partition = 0; partition < Partitions; partition++)
            {
                await eventStore.EventLog.Append($"reducer-replay-{partition}", new ReplayNumberAdded(10));
                last = (await eventStore.EventLog.Append($"reducer-replay-{partition}", new ReplayNumberAdded(20))).SequenceNumber;
            }

            await handler.WaitTillReachesEventSequenceNumber(last, _timeout);
            BeforeReplay = await ReadTotals(eventStore);
            fixture.ReplayCalculation.Multiplier = 2;
            var job = await eventStore.Reducers.Replay<ClusteredReplayReducer>();
            await eventStore.Jobs.WaitTillJobCompletesOrIsDeleted(job, _timeout);
            await handler.WaitTillReachesEventSequenceNumber(last, _timeout);
            AfterReplay = await ReadTotals(eventStore);

            // Every source was processed successfully but now produces no document. This is an intentional empty
            // rebuild, not a replay whose index returned no work, and must replace the populated live model.
            fixture.ReplayCalculation.ReturnEmpty = true;
            job = await eventStore.Reducers.Replay<ClusteredReplayReducer>();
            await eventStore.Jobs.WaitTillJobCompletesOrIsDeleted(job, _timeout);
            await handler.WaitTillReachesEventSequenceNumber(last, _timeout);
            AfterEmptyReplay = await ReadTotals(eventStore);

            fixture.ReplayCalculation.ReturnEmpty = false;
            last = (await eventStore.EventLog.Append("reducer-replay-0", new ReplayNumberAdded(5))).SequenceNumber;
            await handler.WaitTillReachesEventSequenceNumber(last, _timeout);
            AfterLiveAppend = (await eventStore.ReadModels.GetInstanceById<ReplayedTotal>("reducer-replay-0")).Number;
        }

        public Task DisposeAsync()
        {
            fixture.ReplayCalculation.ReturnEmpty = false;
            fixture.ReplayCalculation.Multiplier = 1;
            return Task.CompletedTask;
        }

        static async Task<int[]> ReadTotals(IEventStore eventStore)
        {
            var totals = new int[Partitions];
            for (var partition = 0; partition < Partitions; partition++)
            {
                var model = await eventStore.ReadModels.GetInstanceById<ReplayedTotal>($"reducer-replay-{partition}");
                totals[partition] = model?.Number ?? 0;
            }

            return totals;
        }
    }

    [Fact] void should_reduce_all_original_partitions() => _context.BeforeReplay.ShouldContainOnly(Enumerable.Repeat(30, 24));
    [Fact] void should_rebuild_every_partition_from_empty_state() => _context.AfterReplay.ShouldContainOnly(Enumerable.Repeat(60, 24));
    [Fact] void should_promote_an_intentionally_empty_rebuild() => _context.AfterEmptyReplay.ShouldContainOnly(Enumerable.Repeat(0, 24));
    [Fact] void should_resume_live_writes_against_the_promoted_model() => _context.AfterLiveAppend.ShouldEqual(10);
}
