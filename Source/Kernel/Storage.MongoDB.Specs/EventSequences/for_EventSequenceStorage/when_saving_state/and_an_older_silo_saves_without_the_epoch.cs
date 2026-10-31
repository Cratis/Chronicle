// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Storage.MongoDB.Sinks;
using MongoDB.Bson;

namespace Cratis.Chronicle.Storage.MongoDB.EventSequences.for_EventSequenceStorage.when_saving_state;

[Collection(MongoDBCollection.Name)]
public class and_an_older_silo_saves_without_the_epoch(MongoDBFixture fixture) : given.a_state_storage(fixture)
{
    Chronicle.Storage.EventSequences.EventSequenceState _result;

    async Task Establish()
    {
        await _storage.SaveState(new() { MutationEpoch = new EventSequenceMutationEpoch(4) });

        // An older silo updates only the fields it knows about; it must not clear the epoch.
        await _stateDocuments.UpdateOneAsync(
            new BsonDocument("_id", _eventSequenceId.Value),
            new BsonDocument("$set", new BsonDocument("sequenceNumber", 10L)));
    }

    async Task Because() => _result = await _storage.GetState();

    [Fact] void should_keep_the_epoch() => _result.MutationEpoch.Value.ShouldEqual(4UL);
}
