// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Contracts.Observation;
using Cratis.Chronicle.Storage.Observation;

namespace Cratis.Chronicle.Services.Observation.for_ObserverInformationConverters.when_converting_to_contract;

public class and_observer_is_not_replayable : Specification
{
    ObserverInformation _result;
    ObserverDefinition _definition;
    ObserverState _state;

    void Establish()
    {
        _definition = new(
            "observer-1",
            [new EventType("some-event-type", 1)],
            EventSequenceId.Log,
            Concepts.Observation.ObserverType.Reactor,
            Concepts.Observation.ObserverOwner.Client,
            false);

        _state = new(
            "observer-1",
            EventSequenceNumber.First,
            Concepts.Observation.ObserverRunningState.Active,
            new HashSet<Concepts.Keys.Key>(),
            new HashSet<Concepts.Keys.Key>(),
            [],
            false,
            false)
        {
            NextEventSequenceNumber = 42
        };
    }

    void Because() => _result = _definition.ToContract(_state);

    [Fact] void should_set_is_replayable_to_false() => _result.IsReplayable.ShouldBeFalse();
}
