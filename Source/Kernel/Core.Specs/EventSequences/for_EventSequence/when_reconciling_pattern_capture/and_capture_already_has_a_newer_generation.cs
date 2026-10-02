// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Concepts.Observation.Reactors;
using Cratis.Chronicle.Patterns;

namespace Cratis.Chronicle.EventSequences.for_EventSequence.when_reconciling_pattern_capture;

public class and_capture_already_has_a_newer_generation : given.an_event_sequence_with_reconciled_capture
{
    EventType _subscribedType;

    async Task Establish()
    {
        _subscribedType = new EventType(_eventType.Id, 2);
        await _captureObserver.Subscribe<IPatternCaptureSubscriber>(ObserverType.Reactor, [_subscribedType], SiloAddress.Zero, isReplayable: false);
        _captureState.ClearReceivedCalls();
        _appendedEventsQueues.ClearReceivedCalls();
    }

    Task Because() => _silo.TimerRegistry.FireAllAsync();

    [Fact] async Task should_preserve_the_newer_generation() => (await _captureObserver.GetSubscription()).EventTypes.ShouldContainOnly(_subscribedType);
    [Fact] async Task should_repair_the_narrower_stored_generation() => await _captureDefinitions.Received(1).Save(Arg.Is<ReactorDefinition>(definition => definition.EventTypes.Single().EventType == _subscribedType));
    [Fact] async Task should_not_write_observer_state() => await _captureState.DidNotReceive().WriteStateAsync();
    [Fact] async Task should_not_resubscribe_to_the_queue() => await _appendedEventsQueues.DidNotReceive().Subscribe(Arg.Any<ObserverKey>(), Arg.Any<IEnumerable<EventType>>(), Arg.Any<ObserverFilters?>());
}
