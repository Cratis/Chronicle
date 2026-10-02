// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Concepts.Observation.Reactors;
using Cratis.Chronicle.Patterns;

namespace Cratis.Chronicle.EventSequences.for_EventSequence.when_reconciling_pattern_capture;

public class and_capture_already_has_an_extra_type : given.an_event_sequence_with_reconciled_capture
{
    EventType[] _subscribedTypes;

    async Task Establish()
    {
        // Registration installed this type after reconciliation read its registry snapshot.
        _subscribedTypes = [_eventType, new EventType("new-event", EventTypeGeneration.First)];
        await _captureObserver.Subscribe<IPatternCaptureSubscriber>(ObserverType.Reactor, _subscribedTypes, SiloAddress.Zero, isReplayable: false);
        _captureState.ClearReceivedCalls();
        _appendedEventsQueues.ClearReceivedCalls();
    }

    Task Because() => _silo.TimerRegistry.FireAllAsync();

    [Fact] async Task should_preserve_the_subscription() => (await _captureObserver.GetSubscription()).EventTypes.ShouldContainOnly(_subscribedTypes);
    [Fact] async Task should_not_save_the_reactor_definition() => await _captureDefinitions.DidNotReceive().Save(Arg.Any<ReactorDefinition>());
    [Fact] async Task should_not_write_observer_state() => await _captureState.DidNotReceive().WriteStateAsync();
    [Fact] async Task should_not_resubscribe_to_the_queue() => await _appendedEventsQueues.DidNotReceive().Subscribe(Arg.Any<ObserverKey>(), Arg.Any<IEnumerable<EventType>>(), Arg.Any<ObserverFilters?>());
}
