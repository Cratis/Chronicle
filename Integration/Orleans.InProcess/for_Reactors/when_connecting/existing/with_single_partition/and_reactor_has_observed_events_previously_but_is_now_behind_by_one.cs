// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.Observation;
using Cratis.Chronicle.Reactors;
using context = Cratis.Chronicle.Integration.Orleans.InProcess.for_Reactors.when_connecting.existing.with_single_partition.and_reactor_has_observed_events_previously_but_is_now_behind_by_one.context;

namespace Cratis.Chronicle.Integration.Orleans.InProcess.for_Reactors.when_connecting.existing.with_single_partition;

[Collection(ChronicleCollection.Name)]
public class and_reactor_has_observed_events_previously_but_is_now_behind_by_one(context context) : Given<context>(context)
{
    public class context(ChronicleInProcessFixture chronicleInProcessFixture) : given.a_disconnected_reactor_observing_an_event(chronicleInProcessFixture)
    {
        public List<EventForEventSourceId> FirstEvents;
        public List<EventForEventSourceId> CatchupEvents;

        public ReactorState ReactorState;

        public EventSequenceNumber LastEventSequenceNumberAfterDisconnect;

        async Task Establish()
        {
            var reactor = await EventStore.Reactors.Register<ReactorWithoutDelay>();
            await reactor.WaitTillActive();

            FirstEvents = EventForEventSourceIdHelpers.CreateMultiple(i => new SomeEvent(42), 10, "Partition").ToList();
            var result = await EventStore.EventLog.AppendMany(FirstEvents);
            var lastHandled = result.SequenceNumbers.Last();

            await reactor.WaitTillReachesEventSequenceNumber(lastHandled);
            reactor.Disconnect();

            CatchupEvents = EventForEventSourceIdHelpers.CreateMultiple(i => new SomeEvent(42), 1, "Partition").ToList();
            result = await EventStore.EventLog.AppendMany(CatchupEvents);
            LastEventSequenceNumberAfterDisconnect = result.SequenceNumbers.Last();
        }

        async Task Because()
        {
            var reactor = await EventStore.Reactors.Register<ReactorWithoutDelay>();
            await reactor.WaitTillReachesEventSequenceNumber(LastEventSequenceNumberAfterDisconnect);
            await Reactor.WaitTillHandledEventReaches(FirstEvents.Count + CatchupEvents.Count);
            await reactor.WaitTillActive();
            ReactorState = await reactor.GetState();
        }
    }

    [Fact]
    void should_have_reactor_observer_be_in_running_state() => Context.ReactorState.RunningState.ShouldEqual(ObserverRunningState.Active);

    [Fact]
    void should_catch_up_all_events_added_while_disconnected() => Context.ReactorState.LastHandledEventSequenceNumber.Value.ShouldEqual(Context.LastEventSequenceNumberAfterDisconnect.Value);

    [Fact]
    void should_process_all_events() => Context.Reactor.HandledEvents.ShouldEqual(Context.FirstEvents.Count + Context.CatchupEvents.Count);
}
