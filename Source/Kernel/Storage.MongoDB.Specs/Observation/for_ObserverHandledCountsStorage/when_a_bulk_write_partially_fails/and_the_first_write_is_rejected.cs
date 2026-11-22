// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Observation;

namespace Cratis.Chronicle.Storage.MongoDB.Observation.for_ObserverHandledCountsStorage.when_a_bulk_write_partially_fails;

public class and_the_first_write_is_rejected : given.a_confirmed_bulk_failure
{
    protected override int FailedIndex => 0;

    [Fact] void should_include_only_the_leading_empty_entry() => ((ObserverHandledCountsPartiallyIncremented)_error).AppliedCount.ShouldEqual(1);
    [Fact] void should_retry_every_nonempty_entry() => _attempts[1].Length.ShouldEqual(3);
    [Fact] void should_apply_every_partition_once() => _applied.Values.Order().ShouldEqual(new long[] { 2, 3, 4 });
}
