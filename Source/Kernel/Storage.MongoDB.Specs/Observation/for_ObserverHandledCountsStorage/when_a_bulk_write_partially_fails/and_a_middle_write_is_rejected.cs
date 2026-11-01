// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Observation;
using MongoDB.Driver;

namespace Cratis.Chronicle.Storage.MongoDB.Observation.for_ObserverHandledCountsStorage.when_a_bulk_write_partially_fails;

public class and_a_middle_write_is_rejected : given.a_bulk_write_that_fails
{
    void Establish() => _failure = models => Rejected(models, 1);

    async Task Because() => _error = await Catch.Exception(() => _storage.Increment(_observerId, _counts));

    [Fact] void should_report_a_confirmed_partial_increment() => _error.ShouldBeOfExactType<ObserverHandledCountsPartiallyIncremented>();
    [Fact] void should_include_the_skipped_empty_entry_in_the_applied_prefix() => ((ObserverHandledCountsPartiallyIncremented)_error).AppliedCount.ShouldEqual(2);
    [Fact] void should_preserve_the_driver_error() => _error.InnerException.ShouldBeSame(_driverError);
    [Fact] void should_not_retry_the_bulk_write() => _collection.Received(1).BulkWriteAsync(Arg.Any<IEnumerable<WriteModel<ObserverPartitionCounts>>>(), Arg.Any<BulkWriteOptions>(), Arg.Any<CancellationToken>());
}
