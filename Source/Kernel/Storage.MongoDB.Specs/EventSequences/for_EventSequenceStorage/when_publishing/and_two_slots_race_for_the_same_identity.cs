// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Storage.EventSequences;
using Cratis.Monads;
using MongoDB.Driver;

namespace Cratis.Chronicle.Storage.MongoDB.EventSequences.for_EventSequenceStorage.when_publishing;

[Collection(ReplicaSetMongoDBCollection.Name)]
public class and_two_slots_race_for_the_same_identity(ReplicaSetMongoDBFixture fixture) : a_publication(fixture)
{
    Result<EventPublicationReceipt, DuplicateEventSequenceNumber>[] _results;
    long _count;

    async Task Because()
    {
        _results = await Task.WhenAll(
            Task.Run(() => _storage.AppendPublication(_publication, PublicationEvent(EventSequenceNumber.First))),
            Task.Run(() => _storage.AppendPublication(_publication, PublicationEvent(1))));
        _count = await _collection.CountDocumentsAsync(FilterDefinition<Event>.Empty);
    }

    [Fact] void should_commit_exactly_one_event() => _count.ShouldEqual(1L);
    [Fact] void should_return_success_to_both_callers() => _results.All(_ => _.IsSuccess).ShouldBeTrue();
    [Fact] void should_return_the_same_durable_slot() => _results.Select(_ => _.AsT0.SequenceNumber).Distinct().Count().ShouldEqual(1);
}
