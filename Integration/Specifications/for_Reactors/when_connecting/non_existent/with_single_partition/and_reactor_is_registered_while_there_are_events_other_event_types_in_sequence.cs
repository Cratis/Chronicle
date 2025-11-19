// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.Observation;
using Cratis.Chronicle.Reactors;
using context = Cratis.Chronicle.Integration.Specifications.for_Reactors.when_connecting.non_existent.with_single_partition.and_reactor_is_registered_while_there_are_events_other_event_types_in_sequence.context;

namespace Cratis.Chronicle.Integration.Specifications.for_Reactors.when_connecting.non_existent.with_single_partition;

[Collection(ChronicleCollection.Name)]
public class and_reactor_is_registered_while_there_are_events_other_event_types_in_sequence(context context) : Given<context>(context)
{
    public class context(ChronicleFixture chronicleFixture) : given.a_disconnected_reactor_observing_an_event(chronicleFixture)
    {
        public List<EventForEventSourceId> Events;
        public ReactorState ReactorState;

        public EventSequenceNumber LastEventSequenceNumberAfterDisconnect;

        async Task Establish()
        {
            Events = EventForEventSourceIdHelpers.CreateMultiple(i => new SomeOtherEvent(42), 10, "Partition").ToList();
            var result = await EventStore.EventLog.AppendMany(Events);
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
    void should_catch_up_none_events() => Context.ReactorState.LastHandledEventSequenceNumber.Value.ShouldEqual(EventSequenceNumber.Unavailable.Value);

    [Fact]
    void should_process_all_events() => Context.Reactor.HandledEvents.ShouldEqual(0);
}
