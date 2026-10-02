// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Concepts.Observation.Reactors;
using Cratis.Chronicle.Observation.States;

namespace Cratis.Chronicle.EventSequences.for_EventSequence.when_reconciling_pattern_capture;

public class and_capture_is_quarantined_after_the_recovery_hint : given.an_event_sequence_with_reconciled_capture
{
    async Task Establish()
    {
        await SubscribeCapture();
        _registeredTypes = [_eventType, new EventType("new-event", EventTypeGeneration.First)];
        _captureDefinitions.Save(Arg.Any<ReactorDefinition>()).Returns(async _ =>
        {
            // Quarantine wins the observer turn while the caller is saving the definition after its hint.
            await _captureObserver.TransitionTo<QuarantinedObserver>();
            _captureState.ClearReceivedCalls();
            _appendedEventsQueues.ClearReceivedCalls();
        });
    }

    Task Because() => _silo.TimerRegistry.FireAllAsync();

    [Fact] async Task should_have_passed_the_hint() => await _captureDefinitions.Received(1).Save(Arg.Any<ReactorDefinition>());
    [Fact] async Task should_keep_the_quarantine() => (await _captureObserver.GetCurrentState()).ShouldBeOfExactType<QuarantinedObserver>();
    [Fact] async Task should_not_update_the_subscription() => (await _captureObserver.GetSubscription()).EventTypes.ShouldContainOnly(_eventType);
    [Fact] async Task should_not_write_recovery_state() => await _captureState.DidNotReceive().WriteStateAsync();
    [Fact] async Task should_not_resubscribe_to_the_queue() => await _appendedEventsQueues.DidNotReceive().Subscribe(Arg.Any<ObserverKey>(), Arg.Any<IEnumerable<EventType>>(), Arg.Any<ObserverFilters?>());
}
