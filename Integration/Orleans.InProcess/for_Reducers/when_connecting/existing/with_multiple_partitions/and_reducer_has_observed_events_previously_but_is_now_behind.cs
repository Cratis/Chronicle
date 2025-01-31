// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Jobs;
using Cratis.Chronicle.Contracts.Observation;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.Grains.Observation;
using Cratis.Chronicle.Grains.Observation.Jobs;
using Cratis.Chronicle.Integration.Base;
using Cratis.Chronicle.Storage.Observation;
using Humanizer;
using context = Cratis.Chronicle.Integration.Orleans.InProcess.for_Reducers.when_connecting.existing.with_multiple_partitions.and_reducer_has_observed_events_previously_but_is_now_behind.context;
using ObserverRunningState = Cratis.Chronicle.Concepts.Observation.ObserverRunningState;

namespace Cratis.Chronicle.Integration.Orleans.InProcess.for_Reducers.when_connecting.existing.with_multiple_partitions;

[Collection(GlobalCollection.Name)]
public class and_reducer_has_observed_events_previously_but_is_now_behind(context context) : Given<context>(context)
{
    public class context(GlobalFixture globalFixture) : given.a_disconnected_reducer_observing_an_event(globalFixture)
    {
        public List<EventForEventSourceId> FirstEvents;
        public List<EventForEventSourceId> CatchupEvents;
        public ObserverState ReducerObserverState;

        public EventSequenceNumber LastEventSequenceNumberAfterDisconnect;

        async Task Establish()
        {
            var reactor = await EventStore.Reducers.Register<ReducerWithoutDelay, SomeReadModel>();
            ReducerObserver = GetObserverForReducer<ReducerWithoutDelay>();
            await ReducerObserver.WaitTillActive();

            FirstEvents = EventForEventSourceIdHelpers.CreateMultiple(i => new SomeEvent(42), 10).ToList();
            var result = await EventStore.EventLog.AppendMany(FirstEvents);
            var lastHandled = result.SequenceNumbers.Last();

            await ReducerObserver.WaitTillReachesEventSequenceNumber(lastHandled);
            reactor.Disconnect();

            CatchupEvents = EventForEventSourceIdHelpers.CreateMultiple(i => new SomeEvent(42), 10).ToList();
            result = await EventStore.EventLog.AppendMany(CatchupEvents);
            LastEventSequenceNumberAfterDisconnect = result.SequenceNumbers.Last();
        }

        async Task Because()
        {
            await EventStore.Reducers.Register<ReducerWithoutDelay, SomeReadModel>();
            await ReducerObserver.WaitTillReachesEventSequenceNumber(LastEventSequenceNumberAfterDisconnect);
            await ReducerObserver.WaitTillActive();
            ReducerObserverState = await ReducerObserver.GetState();
        }
    }

    [Fact]
    void should_have_reactor_observer_be_in_running_state() => Context.ReducerObserverState.RunningState.ShouldEqual(ObserverRunningState.Active);

    [Fact]
    void should_catch_up_all_events_added_while_disconnected() => Context.ReducerObserverState.LastHandledEventSequenceNumber.Value.ShouldEqual(Context.LastEventSequenceNumberAfterDisconnect.Value);

    [Fact]
    void should_process_all_events() => Context.Reducer.HandledEvents.ShouldEqual(Context.FirstEvents.Count + Context.CatchupEvents.Count);
}
