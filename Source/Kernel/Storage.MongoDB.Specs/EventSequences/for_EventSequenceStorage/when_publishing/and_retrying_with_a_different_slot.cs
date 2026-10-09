// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Storage.EventSequences;
using Cratis.Monads;
using MongoDB.Driver;

namespace Cratis.Chronicle.Storage.MongoDB.EventSequences.for_EventSequenceStorage.when_publishing;

[Collection(ReplicaSetMongoDBCollection.Name)]
public class and_retrying_with_a_different_slot(ReplicaSetMongoDBFixture fixture) : a_publication(fixture)
{
    Result<EventPublicationReceipt, DuplicateEventSequenceNumber> _result;
    long _count;

    async Task Establish() => (await _storage.AppendPublication(_publication, PublicationEvent(EventSequenceNumber.First))).IsSuccess.ShouldBeTrue();

    async Task Because()
    {
        _result = await _storage.AppendPublication(_publication, PublicationEvent(19));
        _count = await _collection.CountDocumentsAsync(FilterDefinition<Event>.Empty);
    }

    [Fact] void should_return_the_original_slot() => _result.AsT0.SequenceNumber.ShouldEqual(EventSequenceNumber.First);
    [Fact] void should_not_append_again() => _count.ShouldEqual(1L);
}
