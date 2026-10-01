// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.Jobs;
using Cratis.Chronicle.Reducers;
using context = Cratis.Chronicle.Integration.for_Reducers.when_replaying.and_rebuilding_the_read_model.context;

namespace Cratis.Chronicle.Integration.for_Reducers.when_replaying;

[Collection(ChronicleCollection.Name)]
public class and_rebuilding_the_read_model(context context) : Given<context>(context)
{
    public class context(ChronicleFixture fixture) : Specification<ChronicleFixture>(fixture)
    {
        public AccumulatingReducer Reducer;
        public SomeReadModel BeforeReplay;
        public SomeReadModel AfterReplay;
        public SomeReadModel AfterLiveAppend;

        public override IEnumerable<Type> EventTypes => [typeof(SomeEvent)];
        public override IEnumerable<Type> Reducers => [typeof(AccumulatingReducer)];

        protected override void ConfigureServices(IServiceCollection services)
        {
            Reducer = new AccumulatingReducer();
            services.AddSingleton(Reducer);
        }

        async Task Because()
        {
            EventSourceId source = "replayed source";
            var handler = EventStore.Reducers.GetHandlerFor<AccumulatingReducer>();
            await handler.WaitTillActive();
            await EventStore.EventLog.Append(source, new SomeEvent(10));
            var append = await EventStore.EventLog.Append(source, new SomeEvent(20));
            await handler.WaitTillReachesEventSequenceNumber(append.SequenceNumber);
            BeforeReplay = await EventStore.ReadModels.GetInstanceById<SomeReadModel>(source);

            // Changing the calculation makes a replay onto live state (or a watermark-skipped replay) observable.
            Reducer.Multiplier = 2;
            var job = await EventStore.Reducers.Replay<AccumulatingReducer>();
            await EventStore.Jobs.WaitTillJobCompletesOrIsDeleted(job);
            await handler.WaitTillReachesEventSequenceNumber(append.SequenceNumber);
            AfterReplay = await EventStore.ReadModels.GetInstanceById<SomeReadModel>(source);

            append = await EventStore.EventLog.Append(source, new SomeEvent(5));
            await handler.WaitTillReachesEventSequenceNumber(append.SequenceNumber);
            AfterLiveAppend = await EventStore.ReadModels.GetInstanceById<SomeReadModel>(source);
        }
    }

    [Fact] void should_have_reduced_the_original_events() => Context.BeforeReplay.Number.ShouldEqual(30);
    [Fact] void should_rebuild_from_empty_state_using_the_current_reducer() => Context.AfterReplay.Number.ShouldEqual(60);
    [Fact] void should_continue_reducing_into_the_promoted_read_model() => Context.AfterLiveAppend.Number.ShouldEqual(70);
}
