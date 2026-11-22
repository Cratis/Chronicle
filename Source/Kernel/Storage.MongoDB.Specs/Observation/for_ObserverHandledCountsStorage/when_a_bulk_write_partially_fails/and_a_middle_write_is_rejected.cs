// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.MongoDB.Observation.for_ObserverHandledCountsStorage.when_a_bulk_write_partially_fails;

public class and_a_middle_write_is_rejected : given.a_bulk_write_that_fails
{
    void Establish() => _failure = models => Rejected(models, 1);

    async Task Because() => _error = await Catch.Exception(() => _storage.Increment(_observerId, _counts));

    [Fact] void should_translate_the_confirmed_failure() => _error.GetType().Name.ShouldEqual("ObserverHandledCountsPartiallyIncremented");
    [Fact] void should_preserve_the_driver_error() => _error.InnerException.ShouldBeSame(_driverError);
}
