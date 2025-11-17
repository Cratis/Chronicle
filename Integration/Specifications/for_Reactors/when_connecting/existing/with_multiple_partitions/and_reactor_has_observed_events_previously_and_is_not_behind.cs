// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.Observation;
using Cratis.Chronicle.Reactors;
using context = Cratis.Chronicle.Integration.Specifications.for_Reactors.when_connecting.existing.with_multiple_partitions.and_reactor_has_observed_events_previously_and_is_not_behind.context;

namespace Cratis.Chronicle.Integration.Specifications.for_Reactors.when_connecting.existing.with_multiple_partitions;

[Collection(ChronicleCollection.Name)]
public class and_reactor_has_observed_events_previously_and_is_not_behind(context context) : Given<context>(context)
{
    public class context(ChronicleFixture chronicleFixture) : given.a_disconnected_reactor_observing_an_event(chronicleFixture)
    {
        public List<EventForEventSourceId> EventsToHandle;
        public List<EventForEventSourceId> NewEvents;
        public EventSequenceNumber LastHandledEventSequenceNumber;

        public ReactorState ReactorState;

        public EventSequenceNumber LastEventSequenceNumberAfterDisconnect;

        async Task Establish()
        {
            var reactor = await EventStore.Reactors.Register<ReactorWithoutDelay>();
            await reactor.WaitTillActive();

            EventsToHandle = EventForEventSourceIdHelpers.CreateMultiple(i => new SomeEvent(42), 10).ToList();
            var result = await EventStore.EventLog.AppendMany(EventsToHandle);
            LastHandledEventSequenceNumber = result.SequenceNumbers.Last();

            await reactor.WaitTillReachesEventSequenceNumber(LastHandledEventSequenceNumber);
            reactor.Disconnect();

            NewEvents = EventForEventSourceIdHelpers.CreateMultiple(i => new SomeOtherEvent(42), 10).ToList();
            result = await EventStore.EventLog.AppendMany(NewEvents);
            LastEventSequenceNumberAfterDisconnect = result.SequenceNumbers.Last();
        }

        async Task Because()
        {
            var reactor = await EventStore.Reactors.Register<ReactorWithoutDelay>();
            await reactor.WaitTillActive();
            ReactorState = await reactor.GetState();
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
