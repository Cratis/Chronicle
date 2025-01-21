// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Integration.Base;
using Cratis.Chronicle.Storage.Observation;
using context = Cratis.Chronicle.Integration.Orleans.InProcess.for_Reactors.when_connecting.non_existent.with_single_partition.and_reactor_is_registered_while_there_are_no_events_to_handle.context;
using ObserverRunningState = Cratis.Chronicle.Concepts.Observation.ObserverRunningState;

namespace Cratis.Chronicle.Integration.Orleans.InProcess.for_Reactors.when_connecting.non_existent.with_single_partition;

[Collection(GlobalCollection.Name)]
public class and_reactor_is_registered_while_there_are_no_events_to_handle(context context) : Given<context>(context)
{
    public class context(GlobalFixture globalFixture) : given.a_disconnected_reactor_observing_an_event(globalFixture)
    {
        public ObserverState ReactorObserverState;

        async Task Establish()
        {
            var events = EventForEventSourceIdHelpers.CreateMultiple(i => new SomeOtherEvent(42), 10, "Partition").ToList();
            _ = await EventStore.EventLog.AppendMany(events);
        }

        async Task Because()
        {
            ReactorObserver = GetObserverForReactor<ReactorWithoutDelay>();
            await EventStore.Reactors.Register<ReactorWithoutDelay>();
            await ReactorObserver.WaitTillActive();
            ReactorObserverState = await ReactorObserver.GetState();
        }
    }

    [Fact]
    void should_have_reactor_observer_be_in_running_state() => Context.ReactorObserverState.RunningState.ShouldEqual(ObserverRunningState.Active);

    [Fact]
    void should_not_catch_up_any_events() => Context.ReactorObserverState.LastHandledEventSequenceNumber.Value.ShouldEqual(Concepts.Events.EventSequenceNumber.Unavailable.Value);

    [Fact]
    void should_set_next_event_sequence_number_to_first() => Context.ReactorObserverState.NextEventSequenceNumber.Value.ShouldEqual(Concepts.Events.EventSequenceNumber.First.Value);

    [Fact]
    void should_process_no_events() => Context.Reactor.HandledEvents.ShouldEqual(0);
}
