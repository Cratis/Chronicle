// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.Observation;
using Cratis.Chronicle.Reducers;
using context = Cratis.Chronicle.Integration.Specifications.for_Reducers.when_connecting.non_existent.with_multiple_partitions.and_reducer_is_registered_while_there_are_events_in_sequence.context;

namespace Cratis.Chronicle.Integration.Specifications.for_Reducers.when_connecting.non_existent.with_multiple_partitions;

[Collection(ChronicleCollection.Name)]
public class and_reducer_is_registered_while_there_are_events_in_sequence(context context) : Given<context>(context)
{
    public class context(ChronicleFixture chronicleFixture) : given.a_disconnected_reducer_observing_an_event(chronicleFixture)
    {
        public List<EventForEventSourceId> Events;

        public ReducerState ReducerState;

        public EventSequenceNumber LastEventSequenceNumberAppended;

        async Task Establish()
        {
            Events = EventForEventSourceIdHelpers.CreateMultiple(i => new SomeEvent(42), 10).ToList();
            var result = await EventStore.EventLog.AppendMany(Events);
            LastEventSequenceNumberAppended = result.SequenceNumbers.Last();
        }

        async Task Because()
        {
            await EventStore.ReadModels.Register<SomeReadModel>();
            var reducer = await EventStore.Reducers.Register<ReducerWithoutDelay, SomeReadModel>();
            await reducer.WaitTillSubscribed();
            await reducer.WaitTillReachesEventSequenceNumber(LastEventSequenceNumberAppended);

            await Reducer.WaitTillHandledEventReaches(Events.Count);

            ReducerState = await reducer.GetState();
        }
    }

    [Fact]
    void should_have_observer_in_running_state() => Context.ReducerState.RunningState.ShouldEqual(ObserverRunningState.Active);

    [Fact]
    void should_catch_up_all_events_added_while_disconnected() => Context.ReducerState.LastHandledEventSequenceNumber.Value.ShouldEqual(Context.LastEventSequenceNumberAppended.Value);

    [Fact]
    void should_process_all_events() => Context.Reducer.HandledEvents.ShouldEqual(Context.Events.Count);
}
