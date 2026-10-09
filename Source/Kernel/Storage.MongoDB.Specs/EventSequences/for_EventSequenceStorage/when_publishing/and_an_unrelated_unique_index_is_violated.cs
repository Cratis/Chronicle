// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using MongoDB.Driver;

namespace Cratis.Chronicle.Storage.MongoDB.EventSequences.for_EventSequenceStorage.when_publishing;

/// <summary>
/// A duplicate key that is neither the publication identity nor the sequence slot is not something a new slot can
/// cure. It must surface instead of being reported as a slot collision the caller would retry forever.
/// </summary>
/// <param name="fixture">The <see cref="ReplicaSetMongoDBFixture"/>.</param>
[Collection(ReplicaSetMongoDBCollection.Name)]
public class and_an_unrelated_unique_index_is_violated(ReplicaSetMongoDBFixture fixture) : a_publication(fixture)
{
    Exception _error;

    async Task Establish()
    {
        await _collection.Indexes.CreateOneAsync(new CreateIndexModel<Event>(
            Builders<Event>.IndexKeys.Ascending(_ => _.EventSourceId),
            new CreateIndexOptions { Unique = true, Name = "unrelated_unique" }));
        (await _storage.AppendPublication(_publication with { Id = "other" }, PublicationEvent(EventSequenceNumber.First))).IsSuccess.ShouldBeTrue();
    }

    async Task Because() => _error = await Catch.Exception(() => _storage.AppendPublication(_publication, PublicationEvent(1)));

    [Fact] void should_rethrow_the_write_error() => _error.ShouldBeOfExactType<MongoWriteException>();
}
