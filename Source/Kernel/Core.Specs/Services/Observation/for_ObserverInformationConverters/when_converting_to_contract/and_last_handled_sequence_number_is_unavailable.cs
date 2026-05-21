// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Storage.Observation;
using static Cratis.Chronicle.Services.TypeScriptSequenceNumberCompatibility;

namespace Cratis.Chronicle.Services.Observation.for_ObserverInformationConverters.when_converting_to_contract;

public class and_last_handled_sequence_number_is_unavailable : Specification
{
    Contracts.Observation.ObserverInformation _result;
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
            true);

        _state = new(
            "observer-1",
            EventSequenceNumber.Unavailable,
            Concepts.Observation.ObserverRunningState.Active,
            new HashSet<Concepts.Keys.Key>(),
            new HashSet<Concepts.Keys.Key>(),
            [],
            0,
            false,
            false)
        {
            NextEventSequenceNumber = MaxSafeInteger + 1
        };
    }

    void Because() => _result = _definition.ToContract(_state);

    [Fact] void should_sanitize_last_handled_sequence_number() => _result.LastHandledEventSequenceNumber.ShouldEqual(MaxSafeInteger);
    [Fact] void should_sanitize_next_sequence_number() => _result.NextEventSequenceNumber.ShouldEqual(MaxSafeInteger);
}
