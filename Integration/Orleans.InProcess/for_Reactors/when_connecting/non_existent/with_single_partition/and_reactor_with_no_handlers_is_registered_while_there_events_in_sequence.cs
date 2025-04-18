// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.Integration.Base;
using Cratis.Chronicle.Storage.Observation;
using context = Cratis.Chronicle.Integration.Orleans.InProcess.for_Reactors.when_connecting.non_existent.with_single_partition.and_reactor_with_no_handlers_is_registered_while_there_events_in_sequence.context;
using ObserverRunningState = Cratis.Chronicle.Concepts.Observation.ObserverRunningState;

namespace Cratis.Chronicle.Integration.Orleans.InProcess.for_Reactors.when_connecting.non_existent.with_single_partition;

[Collection(GlobalCollection.Name)]
public class and_reactor_with_no_handlers_is_registered_while_there_events_in_sequence(context context) : Given<context>(context)
{
    public class context(GlobalFixture globalFixture) : given.a_disconnected_reactor_observing_no_event_types(globalFixture)
    {
        public List<EventForEventSourceId> Events;
        public ObserverState ReactorObserverState;

        public EventSequenceNumber LastEventSequenceNumberAfterDisconnect;

        async Task Establish()
        {
            Events = EventForEventSourceIdHelpers.CreateMultiple(i => new SomeEvent(42), 10, "Partition").ToList();
            var result = await EventStore.EventLog.AppendMany(Events);
            LastEventSequenceNumberAfterDisconnect = result.SequenceNumbers.Last();
        }

        async Task Because()
        {
            ReactorObserver = GetObserverForReactor<ReactorWithoutHandlers>();
            await EventStore.Reactors.Register<ReactorWithoutHandlers>();
            await ReactorObserver.WaitForState(ObserverRunningState.Disconnected);
            await Task.Delay(500);
            ReactorObserverState = await ReactorObserver.GetState();
        }
    }

    [Fact]
    void should_have_reactor_observer_be_in_disconnected_state() => Context.ReactorObserverState.RunningState.ShouldEqual(ObserverRunningState.Disconnected);

    [Fact]
    void should_not_catch_up_any_events() => Context.ReactorObserverState.LastHandledEventSequenceNumber.Value.ShouldEqual(EventSequenceNumber.Unavailable.Value);
}
