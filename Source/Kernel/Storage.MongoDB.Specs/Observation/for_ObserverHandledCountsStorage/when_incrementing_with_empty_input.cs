// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using MongoDB.Driver;

namespace Cratis.Chronicle.Storage.MongoDB.Observation.for_ObserverHandledCountsStorage;

public class when_incrementing_with_empty_input : given.an_observer_handled_counts_storage
{
    async Task Because() => await _storage.Increment(_observerId, []);

    [Fact] void should_not_write_to_the_collection() => _collection.DidNotReceive().BulkWriteAsync(Arg.Any<IEnumerable<WriteModel<ObserverPartitionCounts>>>(), Arg.Any<BulkWriteOptions>(), Arg.Any<CancellationToken>());
}
