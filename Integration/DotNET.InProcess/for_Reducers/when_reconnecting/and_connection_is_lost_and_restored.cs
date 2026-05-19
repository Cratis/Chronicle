// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.Observation;
using Cratis.Chronicle.Reducers;
using context = Cratis.Chronicle.InProcess.Integration.for_Reducers.when_reconnecting.and_connection_is_lost_and_restored.context;

namespace Cratis.Chronicle.InProcess.Integration.for_Reducers.when_reconnecting;

[Collection(ChronicleCollection.Name)]
public class and_connection_is_lost_and_restored(context context) : Given<context>(context)
{
    public class context(ChronicleInProcessFixture chronicleInProcessFixture) : Specification(chronicleInProcessFixture)
    {
        public EventSourceId EventSourceId;
        public ReducerWithoutDelay Reducer;
        public ReducerState ReducerState;
        public EventSequenceNumber SecondAppendSequenceNumber;
        public IEnumerable<FailedPartition> FailedPartitions = [];

        public override IEnumerable<Type> EventTypes => [typeof(SomeEvent)];
        public override IEnumerable<Type> Reducers => [typeof(ReducerWithoutDelay)];

        protected override void ConfigureServices(IServiceCollection services)
        {
            Reducer = new();
            services.AddSingleton(Reducer);
        }

        void Establish() => EventSourceId = "reducer";

        async Task Because()
        {
            var reducer = EventStore.Reducers.GetHandlerFor<ReducerWithoutDelay>();
            await reducer.WaitTillActive(TimeSpanFactory.FromSeconds(30));

            var firstAppendResult = await EventStore.EventLog.Append(EventSourceId, new SomeEvent(1));
            await reducer.WaitTillReachesEventSequenceNumber(firstAppendResult.SequenceNumber);
            await Reducer.WaitTillHandledEventReaches(1);

            await EventStore.Connection.Lifecycle.Disconnected();
            await EventStore.Connection.Connect();

            reducer = EventStore.Reducers.GetHandlerFor<ReducerWithoutDelay>();
            await reducer.WaitTillActive(TimeSpanFactory.FromSeconds(30));

            var secondAppendResult = await EventStore.EventLog.Append(EventSourceId, new SomeEvent(2));
            SecondAppendSequenceNumber = secondAppendResult.SequenceNumber;
            await reducer.WaitTillReachesEventSequenceNumber(SecondAppendSequenceNumber);
            await Reducer.WaitTillHandledEventReaches(2);

            ReducerState = await reducer.WaitTillActiveAndGetState(TimeSpanFactory.FromSeconds(30));
            FailedPartitions = await reducer.GetFailedPartitions();
        }
    }

    [Fact] Task should_have_two_events_in_the_event_log() => Context.ShouldHaveTailSequenceNumber(Context.SecondAppendSequenceNumber);

    [Fact] void should_continue_handling_events_after_reconnecting() => Context.Reducer.HandledEvents.ShouldEqual(2);

    [Fact] void should_have_reducer_observer_be_active() => Context.ReducerState.RunningState.ShouldEqual(ObserverRunningState.Active);

    [Fact] void should_have_reached_the_last_appended_event() => Context.ReducerState.LastHandledEventSequenceNumber.Value.ShouldEqual(Context.SecondAppendSequenceNumber.Value);

    [Fact] void should_not_have_failed_partitions() => Context.FailedPartitions.ShouldBeEmpty();
}
