// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Observation;

namespace Cratis.Chronicle.Observation.for_Observer.when_recovering_stalled_subscription;

public class and_an_expected_type_is_missing_from_a_newer_subscription : given.an_observer
{
    EventType[] _expectedTypes;
    EventType[] _mergedTypes;

    async Task Establish()
    {
        var oldType = new EventType("some-event", EventTypeGeneration.First);
        var newerType = new EventType(oldType.Id, 2);
        var extraType = new EventType("extra-event", EventTypeGeneration.First);
        var missingType = new EventType("missing-event", EventTypeGeneration.First);
        _expectedTypes = [oldType, missingType];
        _mergedTypes = [newerType, extraType, missingType];
        await _observer.Subscribe<NullObserverSubscriber>(ObserverType.Reactor, [newerType, extraType], SiloAddress.Zero, isReplayable: false);
        _appendedEventsQueues.ClearReceivedCalls();
    }

    Task Because() => _observer.RecoverStalledSubscription<NullObserverSubscriber>(ObserverType.Reactor, _expectedTypes, SiloAddress.Zero, isReplayable: false);

    [Fact] async Task should_preserve_newer_types_in_the_subscription() => (await _observer.GetSubscription()).EventTypes.ShouldContainOnly(_mergedTypes);
    [Fact] void should_preserve_newer_types_in_the_observer_definition() => _definitionStorage.State.EventTypes.ShouldContainOnly(_mergedTypes);
    [Fact] async Task should_subscribe_the_queue_to_the_merged_types() => await _appendedEventsQueues.Received(1).Subscribe(Arg.Any<ObserverKey>(), Arg.Is<IEnumerable<EventType>>(types => types.ToHashSet().SetEquals(_mergedTypes)), Arg.Any<ObserverFilters?>());
}
