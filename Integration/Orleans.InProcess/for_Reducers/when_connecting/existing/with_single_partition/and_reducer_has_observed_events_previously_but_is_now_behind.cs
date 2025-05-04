// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.Integration.Base;
using Cratis.Chronicle.Observation;
using Cratis.Chronicle.Reducers;
using context = Cratis.Chronicle.Integration.Orleans.InProcess.for_Reducers.when_connecting.existing.with_single_partition.and_reducer_has_observed_events_previously_but_is_now_behind.context;

namespace Cratis.Chronicle.Integration.Orleans.InProcess.for_Reducers.when_connecting.existing.with_single_partition;

[Collection(GlobalCollection.Name)]
public class and_reducer_has_observed_events_previously_but_is_now_behind(context context) : Given<context>(context)
{
    public class context(GlobalFixture globalFixture) : given.a_disconnected_reducer_observing_an_event(globalFixture)
    {
        public List<EventForEventSourceId> FirstEvents;
        public List<EventForEventSourceId> CatchupEvents;

        public SomeReducer Reactor;
        public ReducerState ReducerState;
        public EventSequenceNumber LastEventSequenceNumberAfterDisconnect;
        public IEnumerable<FailedPartition> FailedPartitions;

        async Task Establish()
        {
            var reducer = await EventStore.Reducers.Register<ReducerWithoutDelay, SomeReadModel>();
            await EventStore.Reducers.WaitTillSubscribed<ReducerWithoutDelay>();

            FirstEvents = EventForEventSourceIdHelpers.CreateMultiple(i => new SomeEvent(42), 10, "Partition").ToList();
            var result = await EventStore.EventLog.AppendMany(FirstEvents);
            var lastHandled = result.SequenceNumbers.Last();

            await EventStore.Reducers.WaitTillReachesEventSequenceNumber<ReducerWithoutDelay>(lastHandled);
            reducer.Disconnect();

            CatchupEvents = EventForEventSourceIdHelpers.CreateMultiple(i => new SomeEvent(42), 10, "Partition").ToList();
            result = await EventStore.EventLog.AppendMany(CatchupEvents);
            LastEventSequenceNumberAfterDisconnect = result.SequenceNumbers.Last();
        }

        async Task Because()
        {
            await EventStore.Reducers.Register<ReducerWithoutDelay, SomeReadModel>();
            await EventStore.Reducers.WaitTillSubscribed<ReducerWithoutDelay>();
            await EventStore.Reducers.WaitTillReachesEventSequenceNumber<ReducerWithoutDelay>(LastEventSequenceNumberAfterDisconnect);
            ReducerState = await EventStore.Reducers.GetStateFor<ReducerWithoutDelay>();
            FailedPartitions = await EventStore.FailedPartitions.GetAllFailedPartitions();
        }
    }

    [Fact]
    void should_have_reactor_observer_be_in_running_state() => Context.ReducerState.RunningState.ShouldEqual(ObserverRunningState.Active);

    [Fact]
    void should_catch_up_all_events_added_while_disconnected() => Context.ReducerState.LastHandledEventSequenceNumber.Value.ShouldEqual(Context.LastEventSequenceNumberAfterDisconnect.Value);

    [Fact]
    void should_process_all_events() => Context.Reducer.HandledEvents.ShouldEqual(Context.FirstEvents.Count + Context.CatchupEvents.Count);
}
