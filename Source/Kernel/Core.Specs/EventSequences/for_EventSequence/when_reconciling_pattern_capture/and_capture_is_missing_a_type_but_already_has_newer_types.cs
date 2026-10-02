// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Concepts.Observation.Reactors;
using Cratis.Chronicle.Patterns;

namespace Cratis.Chronicle.EventSequences.for_EventSequence.when_reconciling_pattern_capture;

public class and_capture_is_missing_a_type_but_already_has_newer_types : given.an_event_sequence_with_reconciled_capture
{
    EventType[] _mergedTypes;

    async Task Establish()
    {
        var newerType = new EventType(_eventType.Id, 2);
        var extraType = new EventType("extra-event", EventTypeGeneration.First);
        var missingType = new EventType("missing-event", EventTypeGeneration.First);
        await _captureObserver.Subscribe<IPatternCaptureSubscriber>(ObserverType.Reactor, [newerType, extraType], SiloAddress.Zero, isReplayable: false);
        _registeredTypes = [_eventType, missingType];
        _mergedTypes = [newerType, extraType, missingType];
        _appendedEventsQueues.ClearReceivedCalls();
    }

    Task Because() => _silo.TimerRegistry.FireAllAsync();

    [Fact] async Task should_merge_the_subscription_types() => (await _captureObserver.GetSubscription()).EventTypes.ShouldContainOnly(_mergedTypes);
    [Fact] async Task should_save_the_merged_reactor_definition() => await _captureDefinitions.Received(1).Save(Arg.Is<ReactorDefinition>(definition => definition.EventTypes.Select(type => type.EventType).ToHashSet().SetEquals(_mergedTypes)));
    [Fact] async Task should_subscribe_the_queue_to_the_merged_types() => await _appendedEventsQueues.Received(1).Subscribe(Arg.Any<ObserverKey>(), Arg.Is<IEnumerable<EventType>>(types => types.ToHashSet().SetEquals(_mergedTypes)), Arg.Any<ObserverFilters?>());
}
