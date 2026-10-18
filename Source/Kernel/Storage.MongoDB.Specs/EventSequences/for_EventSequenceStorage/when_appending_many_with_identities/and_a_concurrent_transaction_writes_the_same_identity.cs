// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Storage.EventSequences;
using MongoDB.Bson;

namespace Cratis.Chronicle.Storage.MongoDB.EventSequences.for_EventSequenceStorage.when_appending_many_with_identities;

[Collection(ReplicaSetMongoDBCollection.Name)]
public class and_a_concurrent_transaction_writes_the_same_identity(ReplicaSetMongoDBFixture fixture) : given.a_keyed_batch_storage(fixture)
{
    Exception _error;

    async Task Because()
    {
        using var session = await _collection.Database.Client.StartSessionAsync();
        session.StartTransaction();
        var competing = new Event(10, CorrelationId.NotSet, [], [], _eventType.Id, DateTimeOffset.UtcNow, EventSourceType.Default, "competing-source", EventStreamType.All, EventStreamId.Default, [], new Dictionary<string, BsonDocument> { ["1"] = new() }, new Dictionary<string, string>(), [])
        {
            PublicationId = _publication.Id,
            PublicationFingerprint = _publication.Fingerprint
        };
        await _collection.InsertOneAsync(session, competing);
        try
        {
            _error = await Catch.Exception(() => _storage.AppendManyWithIdentities([_event with { Identity = _publication }]));
        }
        finally
        {
            await session.AbortTransactionAsync();
        }
    }

    [Fact] void should_report_a_proven_non_commit() => _error.ShouldBeOfExactType<EventsNotCommitted>();
    [Fact] async Task should_not_commit_the_attempted_event() => (await _storage.GetCount()).Value.ShouldEqual(0UL);
    [Fact] async Task should_not_invent_a_receipt_for_the_uncommitted_winner() => (await _storage.TryGetPublications([_publication.Id])).Count.ShouldEqual(0);
}
