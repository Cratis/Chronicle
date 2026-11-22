// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Observation;

namespace Cratis.Chronicle.Storage.MongoDB.Observation.for_ObserverHandledCountsStorage.when_a_bulk_write_partially_fails;

public class and_a_middle_write_is_rejected : given.a_confirmed_bulk_failure
{
    [Fact] void should_translate_the_confirmed_failure() => _error.ShouldBeOfExactType<ObserverHandledCountsPartiallyIncremented>();
    [Fact] void should_map_the_prefix_to_original_input_including_empty_entries() => ((ObserverHandledCountsPartiallyIncremented)_error).AppliedCount.ShouldEqual(3);
    [Fact] void should_preserve_the_driver_error() => _error.InnerException.ShouldBeSame(_driverError);
    [Fact] void should_retry_only_the_two_remaining_models() => _attempts[1].Length.ShouldEqual(2);
    [Fact] void should_explicitly_order_both_attempts() => _options.TrueForAll(_ => _.IsOrdered == true).ShouldBeTrue();
    [Fact] void should_apply_every_partition_once() => _applied.Values.Order().ShouldEqual(new long[] { 2, 3, 4 });
    [Fact] void should_not_retry_internally() => _attempts.Count.ShouldEqual(2);
}
