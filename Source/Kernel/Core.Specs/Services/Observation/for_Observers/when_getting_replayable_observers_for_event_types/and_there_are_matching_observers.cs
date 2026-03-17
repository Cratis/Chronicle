// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Contracts.Observation;
using Cratis.Chronicle.Storage.Observation;

namespace Cratis.Chronicle.Services.Observation.for_Observers.when_getting_replayable_observers_for_event_types;

public class and_there_are_matching_observers : given.all_dependencies
{
    IEnumerable<ObserverInformation> _result;
    ObserverDefinition _replayableDefinition;
    ObserverState _matchingState;
    EventType _eventType;

    void Establish()
    {
        _eventType = new("some-event-type", 1);

        _replayableDefinition = new(
            "observer-1",
            [_eventType],
            EventSequenceId.Log,
            Concepts.Observation.ObserverType.Reactor,
            Concepts.Observation.ObserverOwner.Client,
            true);

        _matchingState = new(
            "observer-1",
            EventSequenceNumber.First,
            Concepts.Observation.ObserverRunningState.Active,
            new HashSet<Concepts.Keys.Key>(),
            new HashSet<Concepts.Keys.Key>(),
            [],
            false)
        {
            NextEventSequenceNumber = 42
        };

        _observerDefinitionsStorage
            .GetReplayableObserversForEventTypes(Arg.Any<IEnumerable<EventType>>())
            .Returns([_replayableDefinition]);

        _observerStateStorage.GetAll().Returns([_matchingState]);
    }

    async Task Because() => _result = await _observers.GetReplayableObserversForEventTypes(
        new GetReplayableObserversForEventTypesRequest
        {
            EventStore = "some-event-store",
            Namespace = "some-namespace",
            EventTypes = [_eventType.ToContract()]
        });

    [Fact] void should_return_one_observer() => _result.Count().ShouldEqual(1);
    [Fact] void should_have_correct_observer_id() => _result.First().Id.ShouldEqual(_replayableDefinition.Identifier.Value);
    [Fact] void should_be_marked_as_replayable() => _result.First().IsReplayable.ShouldBeTrue();
    [Fact] void should_have_correct_type() => _result.First().Type.ShouldEqual(Contracts.Observation.ObserverType.Reactor);
}
