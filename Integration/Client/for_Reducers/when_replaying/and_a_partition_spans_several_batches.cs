// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.Jobs;
using Cratis.Chronicle.Reducers;
using context = Cratis.Chronicle.Integration.for_Reducers.when_replaying.and_a_partition_spans_several_batches.context;

namespace Cratis.Chronicle.Integration.for_Reducers.when_replaying;

[Collection(ChronicleCollection.Name)]
public class and_a_partition_spans_several_batches(context context) : Given<context>(context)
{
    public class context(ChronicleFixture chronicleFixture) : Specification<ChronicleFixture>(chronicleFixture)
    {
        /// <summary>
        /// More events than one cursor batch holds, so the replay reduces this partition in several batches and each
        /// batch after the first starts from the state the one before it left.
        /// </summary>
        public const int EventCount = 250;

        public static readonly EventSourceId Source = "8d2c50b3-4f4e-4a0e-9d6a-1d3d4f5a0d01";

        public WeightingReducer Reducer = new();
        public ReplayedTotal Total;
        public IEnumerable<Observation.FailedPartition> FailedPartitions;

        public override IEnumerable<Type> EventTypes => [typeof(ReplayNumberAdded)];
        public override IEnumerable<Type> Reducers => [typeof(WeightingReducer)];

        protected override void ConfigureServices(IServiceCollection services) => services.AddSingleton(Reducer);

        async Task Establish()
        {
            var reducer = EventStore.Reducers.GetHandlerFor<WeightingReducer>();
            await reducer.WaitTillSubscribed();

            var last = EventSequenceNumber.First;
            for (var number = 1; number <= EventCount; number++)
            {
                last = (await EventStore.EventLog.Append(Source, new ReplayNumberAdded(number))).SequenceNumber;
            }

            await reducer.WaitTillReachesEventSequenceNumber(last);
            Reducer.Weight = 10;
        }

        async Task Because()
        {
            var jobId = await EventStore.Reducers.Replay<WeightingReducer>();
            await EventStore.Jobs.WaitTillJobCompletesOrIsDeleted(jobId);
            await EventStore.Reducers.GetHandlerFor<WeightingReducer>().WaitTillActive();

            Total = await EventStore.ReadModels.GetInstanceById<ReplayedTotal>(Source.Value);
            FailedPartitions = await EventStore.Reducers.GetHandlerFor<WeightingReducer>().GetFailedPartitions();
        }
    }

    [Fact] void should_not_fail_the_partition() => Context.FailedPartitions.ShouldBeEmpty();
    [Fact] void should_rebuild_the_read_model_from_every_event() => Context.Total.Total.ShouldEqual(10 * context.EventCount * (context.EventCount + 1) / 2);
}
