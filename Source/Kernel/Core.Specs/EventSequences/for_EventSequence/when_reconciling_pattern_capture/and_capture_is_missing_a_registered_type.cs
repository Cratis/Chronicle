// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Concepts.Observation.Reactors;
using Cratis.Chronicle.Observation.States;

namespace Cratis.Chronicle.EventSequences.for_EventSequence.when_reconciling_pattern_capture;

public class and_capture_is_missing_a_registered_type : given.an_event_sequence_with_reconciled_capture
{
    async Task Establish()
    {
        await SubscribeCapture();
        _registeredTypes = [_eventType, new EventType("new-event", EventTypeGeneration.First)];
        _captureState.ClearReceivedCalls();
        _appendedEventsQueues.ClearReceivedCalls();
    }

    Task Because() => _silo.TimerRegistry.FireAllAsync();

    [Fact] async Task should_update_the_subscription() => (await _captureObserver.GetSubscription()).EventTypes.ShouldContainOnly(_registeredTypes);
    [Fact] async Task should_update_the_reactor_definition() => await _captureDefinitions.Received(1).Save(Arg.Is<ReactorDefinition>(definition => definition.EventTypes.Count() == 2));
    [Fact] async Task should_update_the_queue_subscription() => await _appendedEventsQueues.Received(1).Subscribe(Arg.Any<ObserverKey>(), Arg.Is<IEnumerable<EventType>>(types => types.ToHashSet().SetEquals(_registeredTypes)), Arg.Any<ObserverFilters?>());
    [Fact] async Task should_resume_observing() => (await _captureObserver.GetCurrentState()).ShouldBeOfExactType<Observing>();
    [Fact] async Task should_preserve_progress() => (await _captureObserver.GetState()).NextEventSequenceNumber.ShouldEqual((EventSequenceNumber)43UL);
    [Fact] async Task should_not_reload_stale_state() => await _captureState.DidNotReceive().ReadStateAsync();
}
