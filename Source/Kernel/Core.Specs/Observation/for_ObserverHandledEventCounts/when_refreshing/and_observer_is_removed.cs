// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Storage.Observation;

namespace Cratis.Chronicle.Observation.for_ObserverHandledEventCounts.when_refreshing;

public class and_observer_is_removed : given.all_dependencies
{
    ObserverDefinition _initialDefinition;
    ObserverState _initialState;

    async Task Establish()
    {
        _initialDefinition = new(
            "observer-removed",
            [new EventType("event-type-1", 1)],
            EventSequenceId.Log,
            Concepts.Observation.ObserverType.Reactor,
            Concepts.Observation.ObserverOwner.Client,
            true);
        _initialState = new(
            "observer-removed",
            5UL,
            Concepts.Observation.ObserverRunningState.Active,
            new HashSet<Concepts.Keys.Key>(),
            new HashSet<Concepts.Keys.Key>(),
            [],
            0,
            false,
            false);

        _observerDefinitionsStorage.GetAll().Returns([_initialDefinition]);
        _observerStateStorage.GetAll().Returns([_initialState]);
        _eventSequenceStorage.GetCount(Arg.Any<EventSequenceNumber>(), Arg.Any<IEnumerable<EventType>?>()).Returns(new EventCount(6UL));

        // Prime the cache with the initial observer.
        await _grain.Refresh();

        // Simulate the observer being removed from storage.
        _observerDefinitionsStorage.GetAll().Returns([]);
        _observerStateStorage.GetAll().Returns([]);
    }

    async Task Because() => await _grain.Refresh();

    [Fact]
    async Task should_remove_observer_from_cache()
    {
        var counts = await _grain.GetAll();
        counts.ContainsKey(new ObserverHandledEventCountKey(_initialDefinition.Identifier, _initialDefinition.EventSequenceId)).ShouldBeFalse();
    }
}
