// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Contracts.Observation;
using Cratis.Chronicle.Storage.Observation;

namespace Cratis.Chronicle.Services.Observation.for_Observers.when_getting_replayable_observers_for_event_types;

public class and_no_observer_states_match : given.all_dependencies
{
    IEnumerable<ObserverInformation> _result;
    ObserverDefinition _replayableDefinition;
    ObserverState _unmatchedState;
    EventType _eventType;

    void Establish()
    {
        _eventType = new("some-event-type", 1);

        _replayableDefinition = new(
            "observer-1",
            [_eventType],
            Concepts.EventSequences.EventSequenceId.Log,
            Concepts.Observation.ObserverType.Reactor,
            Concepts.Observation.ObserverOwner.Client,
            true);

        _unmatchedState = new(
            "different-observer",
            EventSequenceNumber.First,
            Concepts.Observation.ObserverRunningState.Active,
            new HashSet<Concepts.Keys.Key>(),
            new HashSet<Concepts.Keys.Key>(),
            [],
            false,
            false);

        _observerDefinitionsStorage
            .GetReplayableObserversForEventTypes(Arg.Any<IEnumerable<EventType>>())
            .Returns([_replayableDefinition]);

        _observerStateStorage.GetAll().Returns([_unmatchedState]);
    }

    async Task Because() => _result = await _observers.GetReplayableObserversForEventTypes(
        new GetReplayableObserversForEventTypesRequest
        {
            EventStore = "some-event-store",
            Namespace = "some-namespace",
            EventTypes = [_eventType.ToContract()]
        });

    [Fact] void should_return_empty_result() => _result.ShouldBeEmpty();
}
