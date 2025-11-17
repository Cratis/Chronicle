// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.Observation;
using Cratis.Chronicle.Reactors;
using context = Cratis.Chronicle.Integration.Specifications.for_Reactors.when_connecting.non_existent.with_multiple_partitions.and_reactor_is_registered_while_there_are_events_in_sequence.context;

namespace Cratis.Chronicle.Integration.Specifications.for_Reactors.when_connecting.non_existent.with_multiple_partitions;

[Collection(ChronicleCollection.Name)]
public class and_reactor_is_registered_while_there_are_events_in_sequence(context context) : Given<context>(context)
{
    public class context(ChronicleFixture chronicleFixture) : given.a_disconnected_reactor_observing_an_event(chronicleFixture)
    {
        public List<EventForEventSourceId> Events;
        public ReactorState ReactorState;
        public EventSequenceNumber LastEventSequenceNumberAppended;

        async Task Establish()
        {
            Events = EventForEventSourceIdHelpers.CreateMultiple(i => new SomeEvent(42), 10).ToList();
            var result = await EventStore.EventLog.AppendMany(Events);
            LastEventSequenceNumberAppended = result.SequenceNumbers.Last();
        }

        async Task Because()
        {
            var reactor = await EventStore.Reactors.Register<ReactorWithoutDelay>();
            await reactor.WaitTillReachesEventSequenceNumber(LastEventSequenceNumberAppended);
            await Reactor.WaitTillHandledEventReaches(Events.Count);
            await reactor.WaitTillActive();
            ReactorState = await reactor.GetState();
        }
    }

    [Fact]
    void should_have_observer_in_running_state() => Context.ReactorState.RunningState.ShouldEqual(ObserverRunningState.Active);

    [Fact]
    void should_catch_up_all_events_added_while_disconnected() => Context.ReactorState.LastHandledEventSequenceNumber.Value.ShouldEqual(Context.LastEventSequenceNumberAppended.Value);

    [Fact]
    void should_process_all_events() => Context.Reactor.HandledEvents.ShouldEqual(Context.Events.Count);
}
