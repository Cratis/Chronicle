// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events.Constraints;
using Cratis.Chronicle.Storage.EventSequences;

namespace Cratis.Chronicle.Storage.InMemory.EventSequences.for_EventSequenceStorage.when_saving_state;

public class and_a_catch_up_refusal_is_set : given.an_event_sequence_storage
{
    EventSequenceState _result;
    ConstraintName[] _names = ["email", "username"];

    async Task Because()
    {
        await _storage.SaveState(new EventSequenceState { ConstraintIndexCatchUpRefusing = _names });
        _result = await _storage.GetState();
    }

    [Fact] void should_round_trip_the_refusing_constraints() => _result.ConstraintIndexCatchUpRefusing.ShouldEqual(_names);
}
