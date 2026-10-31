// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events.Constraints;
using Cratis.Chronicle.Storage.MongoDB.Sinks;

namespace Cratis.Chronicle.Storage.MongoDB.EventSequences.for_EventSequenceStorage.when_saving_state;

[Collection(MongoDBCollection.Name)]
public class and_a_catch_up_refusal_is_set(MongoDBFixture fixture) : given.a_state_storage(fixture)
{
    Chronicle.Storage.EventSequences.EventSequenceState _result;
    ConstraintName[] _names = ["email", "username"];

    async Task Because()
    {
        await _storage.SaveState(new() { ConstraintIndexCatchUpRefusing = _names });
        _result = await _storage.GetState();
    }

    [Fact] void should_round_trip_the_refusing_constraints() => _result.ConstraintIndexCatchUpRefusing.ShouldEqual(_names);
}
