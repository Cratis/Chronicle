// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using MongoDB.Driver;

namespace Cratis.Chronicle.Storage.MongoDB.EventSequences.for_EventSequenceStorage.when_reading_back;

public class with_majority_read_concern : given.an_event_sequence_storage
{
    void Establish()
    {
        _collection.WithReadConcern(Arg.Any<ReadConcern>()).Returns(_collection);
        _collection.WithReadPreference(Arg.Any<ReadPreference>()).Returns(_collection);
        _collection.FindAsync(Arg.Any<FilterDefinition<Event>>(), Arg.Any<FindOptions<Event, Event>>(), Arg.Any<CancellationToken>()).Returns(Substitute.For<IAsyncCursor<Event>>());
    }
    async Task Because() => (await _storage.ReadBackAppend(EventSequenceNumber.First, EventSequenceNumber.First)).Dispose();
    [Fact] void should_use_majority_read_concern() => _collection.Received(1).WithReadConcern(ReadConcern.Majority);
    [Fact] void should_use_primary_read_preference() => _collection.Received(1).WithReadPreference(ReadPreference.Primary);
}
