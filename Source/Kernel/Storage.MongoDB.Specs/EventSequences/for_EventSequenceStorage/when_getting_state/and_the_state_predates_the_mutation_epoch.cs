// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.MongoDB.Sinks;
using MongoDB.Bson;

namespace Cratis.Chronicle.Storage.MongoDB.EventSequences.for_EventSequenceStorage.when_getting_state;

[Collection(MongoDBCollection.Name)]
public class and_the_state_predates_the_mutation_epoch(MongoDBFixture fixture) : given.a_state_storage(fixture)
{
    Chronicle.Storage.EventSequences.EventSequenceState _result;

    Task Establish() => _stateDocuments.InsertOneAsync(new BsonDocument
    {
        { "_id", _eventSequenceId.Value },
        { "sequenceNumber", 42L },
        { "tailSequenceNumberPerEventType", new BsonDocument() }
    });

    async Task Because() => _result = await _storage.GetState();

    [Fact] void should_preserve_the_sequence_number() => _result.SequenceNumber.Value.ShouldEqual(42UL);
    [Fact] void should_start_at_the_initial_epoch() => _result.MutationEpoch.ShouldEqual(Concepts.EventSequences.EventSequenceMutationEpoch.Initial);
}
