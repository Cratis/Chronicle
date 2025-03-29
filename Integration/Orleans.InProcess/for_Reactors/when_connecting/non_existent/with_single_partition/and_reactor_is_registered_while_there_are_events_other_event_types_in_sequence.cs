// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.Integration.Base;
using Cratis.Chronicle.Storage.Observation;
using context = Cratis.Chronicle.Integration.Orleans.InProcess.for_Reactors.when_connecting.non_existent.with_single_partition.and_reactor_is_registered_while_there_are_events_other_event_types_in_sequence.context;
using ObserverRunningState = Cratis.Chronicle.Concepts.Observation.ObserverRunningState;

namespace Cratis.Chronicle.Integration.Orleans.InProcess.for_Reactors.when_connecting.non_existent.with_single_partition;

[Collection(GlobalCollection.Name)]
public class and_reactor_is_registered_while_there_are_events_other_event_types_in_sequence(context context) : Given<context>(context)
{
    public class context(GlobalFixture globalFixture) : given.a_disconnected_reactor_observing_an_event(globalFixture)
    {
        public List<EventForEventSourceId> Events;
        public ObserverState ReactorObserverState;

        public EventSequenceNumber LastEventSequenceNumberAfterDisconnect;

        async Task Establish()
        {
            Events = EventForEventSourceIdHelpers.CreateMultiple(i => new SomeOtherEvent(42), 10, "Partition").ToList();
            var result = await EventStore.EventLog.AppendMany(Events);
            LastEventSequenceNumberAfterDisconnect = result.SequenceNumbers.Last();
        }

        async Task Because()
        {
            ReactorObserver = GetObserverForReactor<ReactorWithoutDelay>();
            await EventStore.Reactors.Register<ReactorWithoutDelay>();
            await Task.Delay(1000);
            await ReactorObserver.WaitTillActive();
            ReactorObserverState = await ReactorObserver.GetState();
        }
    }

    [Fact]
    void should_have_reactor_observer_be_in_running_state() => Context.ReactorObserverState.RunningState.ShouldEqual(ObserverRunningState.Active);

    [Fact]
    void should_catch_up_none_events() => Context.ReactorObserverState.LastHandledEventSequenceNumber.Value.ShouldEqual(EventSequenceNumber.Unavailable.Value);

    [Fact]
    void should_process_all_events() => Context.Reactor.HandledEvents.ShouldEqual(0);
}
