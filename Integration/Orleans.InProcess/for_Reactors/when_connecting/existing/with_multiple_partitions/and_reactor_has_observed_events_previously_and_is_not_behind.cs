// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.Integration.Base;
using Cratis.Chronicle.Observation;
using Cratis.Chronicle.Reactors;
using context = Cratis.Chronicle.Integration.Orleans.InProcess.for_Reactors.when_connecting.existing.with_multiple_partitions.and_reactor_has_observed_events_previously_and_is_not_behind.context;

namespace Cratis.Chronicle.Integration.Orleans.InProcess.for_Reactors.when_connecting.existing.with_multiple_partitions;

[Collection(GlobalCollection.Name)]
public class and_reactor_has_observed_events_previously_and_is_not_behind(context context) : Given<context>(context)
{
    public class context(GlobalFixture globalFixture) : given.a_disconnected_reactor_observing_an_event(globalFixture)
    {
        public List<EventForEventSourceId> EventsToHandle;
        public List<EventForEventSourceId> NewEvents;
        public EventSequenceNumber LastHandledEventSequenceNumber;

        public ReactorState ReactorState;

        public EventSequenceNumber LastEventSequenceNumberAfterDisconnect;

        async Task Establish()
        {
            var reactor = await EventStore.Reactors.Register<ReactorWithoutDelay>();
            await EventStore.Reactors.WaitTillActive<ReactorWithoutDelay>();

            EventsToHandle = EventForEventSourceIdHelpers.CreateMultiple(i => new SomeEvent(42), 10).ToList();
            var result = await EventStore.EventLog.AppendMany(EventsToHandle);
            LastHandledEventSequenceNumber = result.SequenceNumbers.Last();

            await EventStore.Reactors.WaitTillReachesEventSequenceNumber<ReactorWithoutDelay>(LastHandledEventSequenceNumber);
            reactor.Disconnect();

            NewEvents = EventForEventSourceIdHelpers.CreateMultiple(i => new SomeOtherEvent(42), 10).ToList();
            result = await EventStore.EventLog.AppendMany(NewEvents);
            LastEventSequenceNumberAfterDisconnect = result.SequenceNumbers.Last();
        }

        async Task Because()
        {
            await EventStore.Reactors.Register<ReactorWithoutDelay>();
            await EventStore.Reactors.WaitTillActive<ReactorWithoutDelay>();
            ReactorState = await EventStore.Reactors.GetState<ReactorWithoutDelay>();
        }
    }

    [Fact]
    void should_have_reactor_observer_be_in_running_state() => Context.ReactorState.RunningState.ShouldEqual(ObserverRunningState.Active);

    [Fact]
    void should_not_catch_up_any_new_events_added_while_disconnected() => Context.ReactorState.LastHandledEventSequenceNumber.Value.ShouldEqual(Context.LastHandledEventSequenceNumber.Value);

    [Fact]
    void should_set_correct_next_event_sequence_number() => Context.ReactorState.NextEventSequenceNumber.Value.ShouldEqual(Context.LastEventSequenceNumberAfterDisconnect.Next().Value);

    [Fact]
    void should_only_process_first_events() => Context.Reactor.HandledEvents.ShouldEqual(Context.EventsToHandle.Count);
}
