// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Storage.Observation;

namespace Cratis.Chronicle.Observation.for_ObserverHandledEventCounts.when_refreshing;

public class and_observers_have_handled_events : given.all_dependencies
{
    ObserverDefinition _definition;
    ObserverState _state;

    void Establish()
    {
        _definition = new(
            "observer-1",
            [new EventType("event-type-1", 1)],
            EventSequenceId.Log,
            Concepts.Observation.ObserverType.Reactor,
            Concepts.Observation.ObserverOwner.Client,
            true);
        _state = new(
            "observer-1",
            42UL,
            Concepts.Observation.ObserverRunningState.Active,
            new HashSet<Concepts.Keys.Key>(),
            new HashSet<Concepts.Keys.Key>(),
            [],
            0,
            false,
            false);

        _observerDefinitionsStorage.GetAll().Returns([_definition]);
        _observerStateStorage.GetAll().Returns([_state]);
        _eventSequenceStorage.GetCount(Arg.Any<EventSequenceNumber>(), Arg.Any<IEnumerable<EventType>?>()).Returns(new EventCount(43UL));
    }

    async Task Because() => await _grain.Refresh();

    [Fact]
    async Task should_cache_count_for_observer()
    {
        var counts = await _grain.GetAll();
        counts[new ObserverHandledEventCountKey(_definition.Identifier, _definition.EventSequenceId)].ShouldEqual(new EventCount(43UL));
    }

    [Fact]
    async Task should_query_storage_for_count_up_to_last_handled_sequence_number()
    {
        await _grain.Refresh();
        await _eventSequenceStorage.Received().GetCount(
            Arg.Is<EventSequenceNumber>(n => n == _state.LastHandledEventSequenceNumber),
            Arg.Any<IEnumerable<EventType>?>());
    }
}
