// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.MongoDB.Sinks;

namespace Cratis.Chronicle.Storage.MongoDB.EventSequences.for_EventSequenceStorage.when_saving_state;

[Collection(MongoDBCollection.Name)]
public class and_a_mutation_epoch_is_set(MongoDBFixture fixture) : given.a_state_storage(fixture)
{
    Chronicle.Storage.EventSequences.EventSequenceState _result;

    async Task Because()
    {
        await _storage.SaveState(new() { MutationEpoch = 42 });
        _result = await _storage.GetState();
    }

    [Fact] void should_round_trip_the_epoch() => _result.MutationEpoch.Value.ShouldEqual(42UL);
}
