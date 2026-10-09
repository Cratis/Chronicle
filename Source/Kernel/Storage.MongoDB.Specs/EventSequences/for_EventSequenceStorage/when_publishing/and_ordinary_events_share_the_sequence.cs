// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Storage.EventSequences;
using Cratis.Monads;
using MongoDB.Driver;

namespace Cratis.Chronicle.Storage.MongoDB.EventSequences.for_EventSequenceStorage.when_publishing;

[Collection(ReplicaSetMongoDBCollection.Name)]
public class and_ordinary_events_share_the_sequence(ReplicaSetMongoDBFixture fixture) : a_publication(fixture)
{
    Result<EventPublicationReceipt, DuplicateEventSequenceNumber> _result;
    long _count;

    async Task Establish() => (await _storage.AppendMany([EventAt(EventSequenceNumber.First), EventAt(1)])).IsSuccess.ShouldBeTrue();

    async Task Because()
    {
        _result = await _storage.AppendPublication(_publication, PublicationEvent(2));
        _count = await _collection.CountDocumentsAsync(FilterDefinition<Event>.Empty);
    }

    [Fact] void should_publish() => _result.IsSuccess.ShouldBeTrue();
    [Fact] void should_keep_the_ordinary_events() => _count.ShouldEqual(3L);
}
