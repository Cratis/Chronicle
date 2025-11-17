// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.Observation;
using Cratis.Chronicle.Reactors;
using context = Cratis.Chronicle.Integration.Specifications.for_Reactors.when_connecting.non_existent.with_single_partition.and_reactor_with_no_handlers_is_registered_while_there_events_in_sequence.context;

namespace Cratis.Chronicle.Integration.Specifications.for_Reactors.when_connecting.non_existent.with_single_partition;

[Collection(ChronicleCollection.Name)]
public class and_reactor_with_no_handlers_is_registered_while_there_events_in_sequence(context context) : Given<context>(context)
{
    public class context(ChronicleFixture chronicleFixture) : given.a_disconnected_reactor_observing_no_event_types(chronicleFixture)
    {
        public List<EventForEventSourceId> Events;
        public ReactorState ReactorState;

        public EventSequenceNumber LastEventSequenceNumberAfterDisconnect;

        async Task Establish()
        {
            Events = EventForEventSourceIdHelpers.CreateMultiple(i => new SomeEvent(42), 10, "Partition").ToList();
            var result = await EventStore.EventLog.AppendMany(Events);
            LastEventSequenceNumberAfterDisconnect = result.SequenceNumbers.Last();
        }

        async Task Because()
        {
            var reactor = await EventStore.Reactors.Register<ReactorWithoutHandlers>();
            await reactor.WaitForState(ObserverRunningState.Disconnected);
            ReactorState = await reactor.GetState();
        }
    }

    [Fact]
    void should_have_reactor_observer_be_in_disconnected_state() => Context.ReactorState.RunningState.ShouldEqual(ObserverRunningState.Disconnected);

    [Fact]
    void should_not_catch_up_any_events() => Context.ReactorState.LastHandledEventSequenceNumber.Value.ShouldEqual(EventSequenceNumber.Unavailable.Value);
}
