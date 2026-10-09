// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Storage.EventSequences;
using MongoDB.Driver;

namespace Cratis.Chronicle.Storage.MongoDB.EventSequences.for_EventSequenceStorage.when_publishing;

[Collection(ReplicaSetMongoDBCollection.Name)]
public class and_reusing_an_identity_for_a_different_intent(ReplicaSetMongoDBFixture fixture) : a_publication(fixture)
{
    Exception? _error;
    long _count;

    async Task Establish() => (await _storage.AppendPublication(_publication, PublicationEvent(EventSequenceNumber.First))).IsSuccess.ShouldBeTrue();

    async Task Because()
    {
        _error = await Specifications.Catch.Exception(() => _storage.AppendPublication(_publication with { Fingerprint = "different" }, PublicationEvent(1)));
        _count = await _collection.CountDocumentsAsync(FilterDefinition<Event>.Empty);
    }

    [Fact] void should_fail_closed() => _error.ShouldBeOfExactType<EventPublicationConflict>();
    [Fact] void should_not_append_another_event() => _count.ShouldEqual(1L);
}
