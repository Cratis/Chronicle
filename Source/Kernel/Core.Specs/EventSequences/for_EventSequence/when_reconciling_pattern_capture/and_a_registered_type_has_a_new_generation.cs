// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Concepts.Observation.Reactors;

namespace Cratis.Chronicle.EventSequences.for_EventSequence.when_reconciling_pattern_capture;

public class and_a_registered_type_has_a_new_generation : given.an_event_sequence_with_reconciled_capture
{
    async Task Establish()
    {
        await SubscribeCapture();
        _registeredTypes = [new EventType(_eventType.Id, 2)];
        _appendedEventsQueues.ClearReceivedCalls();
    }

    Task Because() => _silo.TimerRegistry.FireAllAsync();

    [Fact] async Task should_update_the_subscription_generation() => (await _captureObserver.GetSubscription()).EventTypes.ShouldContainOnly(_registeredTypes);
    [Fact] async Task should_update_the_reactor_definition() => await _captureDefinitions.Received(1).Save(Arg.Any<ReactorDefinition>());
    [Fact] async Task should_update_the_queue_subscription() => await _appendedEventsQueues.Received(1).Subscribe(Arg.Any<ObserverKey>(), Arg.Is<IEnumerable<EventType>>(types => types.ToHashSet().SetEquals(_registeredTypes)), Arg.Any<ObserverFilters?>());
}
