// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.MongoDB.Observation.for_ObserverHandledCountsStorage.when_a_bulk_write_partially_fails;

public class and_a_write_concern_error_is_reported : given.a_bulk_write_that_fails
{
    void Establish() => _failure = models => Rejected(models, 1, writeConcern: true);

    async Task Because() => _error = await Catch.Exception(() => _storage.Increment(_observerId, _counts));

    [Fact] void should_preserve_the_unknown_outcome() => _error.ShouldBeSame(_driverError);
}
