// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Observation;

namespace Cratis.Chronicle.Storage.MongoDB.Observation.for_ObserverHandledCountsStorage.when_a_bulk_write_partially_fails;

public class and_the_last_write_is_rejected : given.a_confirmed_bulk_failure
{
    protected override int FailedIndex => 2;

    [Fact] void should_exclude_the_failed_and_trailing_empty_entries() => ((ObserverHandledCountsPartiallyIncremented)_error).AppliedCount.ShouldEqual(4);
    [Fact] void should_retry_only_the_last_model() => _attempts[1].Length.ShouldEqual(1);
    [Fact] void should_apply_every_partition_once() => _applied.Values.Order().ShouldEqual(new long[] { 2, 3, 4 });
}
