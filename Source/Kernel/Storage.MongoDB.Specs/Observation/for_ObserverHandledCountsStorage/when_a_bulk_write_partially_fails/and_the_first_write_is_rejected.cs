// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Observation;

namespace Cratis.Chronicle.Storage.MongoDB.Observation.for_ObserverHandledCountsStorage.when_a_bulk_write_partially_fails;

public class and_the_first_write_is_rejected : given.a_bulk_write_that_fails
{
    void Establish() => _failure = models => Rejected(models, 0);

    async Task Because() => _error = await Catch.Exception(() => _storage.Increment(_observerId, _counts));

    [Fact] void should_report_a_confirmed_partial_increment() => _error.ShouldBeOfExactType<ObserverHandledCountsPartiallyIncremented>();
    [Fact] void should_report_no_applied_entries() => ((ObserverHandledCountsPartiallyIncremented)_error).AppliedCount.ShouldEqual(0);
}
