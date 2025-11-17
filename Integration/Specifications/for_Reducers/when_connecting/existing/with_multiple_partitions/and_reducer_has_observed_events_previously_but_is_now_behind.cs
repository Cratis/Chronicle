// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.Observation;
using Cratis.Chronicle.Reducers;
using context = Cratis.Chronicle.Integration.Specifications.for_Reducers.when_connecting.existing.with_multiple_partitions.and_reducer_has_observed_events_previously_but_is_now_behind.context;

namespace Cratis.Chronicle.Integration.Specifications.for_Reducers.when_connecting.existing.with_multiple_partitions;

[Collection(ChronicleCollection.Name)]
public class and_reducer_has_observed_events_previously_but_is_now_behind(context context) : Given<context>(context)
{
    public class context(ChronicleFixture chronicleFixture) : given.a_disconnected_reducer_observing_an_event(chronicleFixture)
    {
        public List<EventForEventSourceId> FirstEvents;
        public List<EventForEventSourceId> CatchupEvents;
        public ReducerState ReducerState;
        public EventSequenceNumber LastEventSequenceNumberAfterDisconnect;

        async Task Establish()
        {
            await EventStore.ReadModels.Register<SomeReadModel>();
            var reducer = await EventStore.Reducers.Register<ReducerWithoutDelay, SomeReadModel>();
            await reducer.WaitTillSubscribed();

            FirstEvents = EventForEventSourceIdHelpers.CreateMultiple(i => new SomeEvent(42), 10).ToList();
            var result = await EventStore.EventLog.AppendMany(FirstEvents);
            var lastHandled = result.SequenceNumbers.Last();

            await reducer.WaitTillReachesEventSequenceNumber(lastHandled);
            reducer.Disconnect();

            CatchupEvents = EventForEventSourceIdHelpers.CreateMultiple(i => new SomeEvent(42), 10).ToList();
            result = await EventStore.EventLog.AppendMany(CatchupEvents);
            LastEventSequenceNumberAfterDisconnect = result.SequenceNumbers.Last();
        }

        async Task Because()
        {
            var reducer = await EventStore.Reducers.Register<ReducerWithoutDelay, SomeReadModel>();
            await reducer.WaitTillSubscribed();
            await reducer.WaitTillReachesEventSequenceNumber(LastEventSequenceNumberAfterDisconnect);
            ReducerState = await reducer.GetState();
        }
    }

    [Fact]
    void should_be_in_running_state() => Context.ReducerState.RunningState.ShouldEqual(ObserverRunningState.Active);

    [Fact]
    void should_catch_up_all_events_added_while_disconnected() => Context.ReducerState.LastHandledEventSequenceNumber.Value.ShouldEqual(Context.LastEventSequenceNumberAfterDisconnect.Value);

    [Fact]
    void should_process_all_events() => Context.Reducer.HandledEvents.ShouldEqual(Context.FirstEvents.Count + Context.CatchupEvents.Count);
}
