// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.Integration.Base;
using Cratis.Chronicle.Observation;
using Cratis.Chronicle.Reactors;
using context = Cratis.Chronicle.Integration.Orleans.InProcess.for_Reactors.when_connecting.non_existent.with_multiple_partition.and_reactor_is_registered_while_there_are_no_events_to_handle.context;

namespace Cratis.Chronicle.Integration.Orleans.InProcess.for_Reactors.when_connecting.non_existent.with_multiple_partition;

[Collection(GlobalCollection.Name)]
public class and_reactor_is_registered_while_there_are_no_events_to_handle(context context) : Given<context>(context)
{
    public class context(GlobalFixture globalFixture) : given.a_disconnected_reactor_observing_an_event(globalFixture)
    {
        public ReactorState ReactorState;
        public EventSequenceNumber LastEventSequenceNumber;

        async Task Establish()
        {
            var events = EventForEventSourceIdHelpers.CreateMultiple(i => new SomeOtherEvent(42), 10).ToList();
            var result = await EventStore.EventLog.AppendMany(events);
            LastEventSequenceNumber = result.SequenceNumbers.Last().Next();
        }

        async Task Because()
        {
            await EventStore.Reactors.Register<ReactorWithoutDelay>();
            await EventStore.Reactors.WaitTillActive<ReactorWithoutDelay>();
            ReactorState = await EventStore.Reactors.GetStateFor<ReactorWithoutDelay>();
        }
    }

    [Fact]
    void should_have_reactor_observer_be_in_running_state() => Context.ReactorState.RunningState.ShouldEqual(ObserverRunningState.Active);

    [Fact]
    void should_not_catch_up_any_events() => Context.ReactorState.LastHandledEventSequenceNumber.Value.ShouldEqual(Concepts.Events.EventSequenceNumber.Unavailable.Value);

    [Fact]
    void should_set_next_event_sequence_number_to_next_after_last_sequence_number_of_appended_events() => Context.ReactorState.NextEventSequenceNumber.Value.ShouldEqual(Context.LastEventSequenceNumber.Value);

    [Fact]
    void should_process_no_events() => Context.Reactor.HandledEvents.ShouldEqual(0);
}
