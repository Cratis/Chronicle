// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.Jobs;
using Cratis.Chronicle.Reducers;
using context = Cratis.Chronicle.Integration.for_Reducers.when_replaying.and_the_reducer_has_changed_since_the_read_model_was_built.context;

namespace Cratis.Chronicle.Integration.for_Reducers.when_replaying;

[Collection(ChronicleCollection.Name)]
public class and_the_reducer_has_changed_since_the_read_model_was_built(context context) : Given<context>(context)
{
    public class context(ChronicleFixture chronicleFixture) : Specification<ChronicleFixture>(chronicleFixture)
    {
        public static readonly EventSourceId FirstSource = "8d2c50b3-4f4e-4a0e-9d6a-1d3d4f5a0b01";
        public static readonly EventSourceId SecondSource = "8d2c50b3-4f4e-4a0e-9d6a-1d3d4f5a0b02";

        public WeightingReducer Reducer = new();
        public ReplayedTotal FirstBeforeReplay;
        public ReplayedTotal First;
        public ReplayedTotal Second;

        public override IEnumerable<Type> EventTypes => [typeof(ReplayNumberAdded)];
        public override IEnumerable<Type> Reducers => [typeof(WeightingReducer)];

        protected override void ConfigureServices(IServiceCollection services) => services.AddSingleton(Reducer);

        async Task Establish()
        {
            var reducer = EventStore.Reducers.GetHandlerFor<WeightingReducer>();
            await reducer.WaitTillSubscribed();

            await EventStore.EventLog.Append(FirstSource, new ReplayNumberAdded(1));
            await EventStore.EventLog.Append(FirstSource, new ReplayNumberAdded(2));
            await EventStore.EventLog.Append(SecondSource, new ReplayNumberAdded(5));
            var last = await EventStore.EventLog.Append(FirstSource, new ReplayNumberAdded(3));
            await reducer.WaitTillReachesEventSequenceNumber(last.SequenceNumber);

            FirstBeforeReplay = await EventStore.ReadModels.GetInstanceById<ReplayedTotal>(FirstSource.Value);
            Reducer.Weight = 10;
        }

        async Task Because()
        {
            var jobId = await EventStore.Reducers.Replay<WeightingReducer>();
            await EventStore.Jobs.WaitTillJobCompletesOrIsDeleted(jobId);
            await EventStore.Reducers.GetHandlerFor<WeightingReducer>().WaitTillActive();

            First = await EventStore.ReadModels.GetInstanceById<ReplayedTotal>(FirstSource.Value);
            Second = await EventStore.ReadModels.GetInstanceById<ReplayedTotal>(SecondSource.Value);
        }
    }

    [Fact] void should_have_built_the_read_model_with_the_original_reducer() => Context.FirstBeforeReplay.Total.ShouldEqual(6);
    [Fact] void should_rebuild_the_first_read_model_with_the_changed_reducer() => Context.First.Total.ShouldEqual(60);
    [Fact] void should_rebuild_the_second_read_model_with_the_changed_reducer() => Context.Second.Total.ShouldEqual(50);
}
