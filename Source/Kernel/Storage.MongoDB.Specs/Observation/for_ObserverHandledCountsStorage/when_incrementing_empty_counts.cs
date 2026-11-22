// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;
using MongoDB.Driver;

namespace Cratis.Chronicle.Storage.MongoDB.Observation.for_ObserverHandledCountsStorage;

public class when_incrementing_empty_counts : given.an_observer_handled_counts_storage
{
    async Task Because()
    {
        await _storage.Increment(_observerId, []);
        await _storage.Increment(
            _observerId,
            [
                new((Key)"first", new Dictionary<EventTypeId, EventCount>()),
                new((Key)"last", new Dictionary<EventTypeId, EventCount>())
            ]);
    }

    [Fact] void should_not_issue_a_bulk_write() => _collection.DidNotReceive().BulkWriteAsync(Arg.Any<IEnumerable<WriteModel<ObserverPartitionCounts>>>(), Arg.Any<BulkWriteOptions>(), Arg.Any<CancellationToken>());
}
