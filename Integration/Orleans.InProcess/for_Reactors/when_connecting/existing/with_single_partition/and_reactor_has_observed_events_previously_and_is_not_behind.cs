// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.Integration.Base;
using Cratis.Chronicle.Storage.Observation;
using context = Cratis.Chronicle.Integration.Orleans.InProcess.for_Reactors.when_connecting.existing.with_single_partition.and_reactor_has_observed_events_previously_and_is_not_behind.context;
using ObserverRunningState = Cratis.Chronicle.Concepts.Observation.ObserverRunningState;

namespace Cratis.Chronicle.Integration.Orleans.InProcess.for_Reactors.when_connecting.existing.with_single_partition;

[Collection(GlobalCollection.Name)]
public class and_reactor_has_observed_events_previously_and_is_not_behind(context context) : Given<context>(context)
{
    public class context(GlobalFixture globalFixture) : given.a_disconnected_reactor_observing_an_event(globalFixture)
    {
        public List<EventForEventSourceId> EventsToHandle;
        public List<EventForEventSourceId> NewEvents;
        public EventSequenceNumber LastHandledEventSequenceNumber;

        public ObserverState ReactorObserverState;

        public EventSequenceNumber LastEventSequenceNumberAfterDisconnect;

        async Task Establish()
        {
            var reactor = await EventStore.Reactors.Register<ReactorWithoutDelay>();
            ReactorObserver = GetObserverForReactor<ReactorWithoutDelay>();
            await ReactorObserver.WaitTillActive();

            EventsToHandle = EventForEventSourceIdHelpers.CreateMultiple(i => new SomeEvent(42), 10, "Partition").ToList();
            var result = await EventStore.EventLog.AppendMany(EventsToHandle);
            LastHandledEventSequenceNumber = result.SequenceNumbers.Last();

            await ReactorObserver.WaitTillReachesEventSequenceNumber(LastHandledEventSequenceNumber);
            reactor.Disconnect();

            NewEvents = EventForEventSourceIdHelpers.CreateMultiple(i => new SomeOtherEvent(42), 10, "Partition").ToList();
            result = await EventStore.EventLog.AppendMany(NewEvents);
            LastEventSequenceNumberAfterDisconnect = result.SequenceNumbers.Last();
        }

        async Task Because()
        {
            await EventStore.Reactors.Register<ReactorWithoutDelay>();
            await ReactorObserver.WaitTillActive();
            ReactorObserverState = await ReactorObserver.GetState();
        }
    }

    [Fact]
    void should_have_reactor_observer_be_in_running_state() => Context.ReactorObserverState.RunningState.ShouldEqual(ObserverRunningState.Active);

    [Fact]
    void should_not_catch_up_any_new_events_added_while_disconnected() => Context.ReactorObserverState.LastHandledEventSequenceNumber.Value.ShouldEqual(Context.LastHandledEventSequenceNumber.Value);

    [Fact]
    void should_set_correct_next_event_sequence_number() => Context.ReactorObserverState.NextEventSequenceNumber.Value.ShouldEqual(Context.LastEventSequenceNumberAfterDisconnect.Next().Value);

    [Fact]
    void should_only_process_first_events() => Context.Reactor.HandledEvents.ShouldEqual(Context.EventsToHandle.Count);
}
