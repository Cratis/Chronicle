// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Identities;
using MongoDB.Driver;

namespace Cratis.Chronicle.Storage.MongoDB.EventSequences.for_EventSequenceStorage.when_revising;

[Collection(ReplicaSetMongoDBCollection.Name)]
public class and_the_event_was_redacted(ReplicaSetMongoDBFixture fixture) : given.a_replica_set_event_sequence_storage(fixture)
{
    Event _stored;

    async Task Establish()
    {
        (await _storage.AppendMany([EventAt(0)])).IsSuccess.ShouldBeTrue();
        await _storage.Redact(0, "reason", CorrelationId.New(), [], [], DateTimeOffset.UtcNow);
    }

    async Task Because()
    {
        await _storage.Revise(0, _eventType, CorrelationId.New(), [], [IdentityId.NotSet], DateTimeOffset.UtcNow, new ExpandoObject(), new EventHash("revised"));
        _stored = await _collection.Find(@event => @event.SequenceNumber == 0).SingleAsync();
    }

    [Fact] void should_not_attach_a_revision_to_the_redacted_event() => _stored.Revisions.ShouldBeEmpty();
    [Fact] void should_keep_the_redaction_type() => _stored.Type.ShouldEqual(GlobalEventTypes.Redaction);
}
