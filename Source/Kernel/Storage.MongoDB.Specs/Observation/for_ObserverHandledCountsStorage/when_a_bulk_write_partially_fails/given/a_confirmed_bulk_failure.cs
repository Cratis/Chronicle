// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.MongoDB.Observation.for_ObserverHandledCountsStorage.when_a_bulk_write_partially_fails.given;

public class a_confirmed_bulk_failure : a_bulk_write_that_fails
{
    protected virtual int FailedIndex => 1;

    void Establish() => _failure = models => Rejected(models, FailedIndex);
}
