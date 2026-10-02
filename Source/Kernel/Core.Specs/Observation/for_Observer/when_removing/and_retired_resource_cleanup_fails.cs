// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation;

namespace Cratis.Chronicle.Observation.for_Observer.when_removing;

public class and_retired_resource_cleanup_fails : given.an_observer
{
    Exception _error;
    readonly Exception _failure = new("Cleanup failed");

    async Task Establish()
    {
        await _observer.Retire();
        _eventStoreNamespaceStorage.FailedPartitions.RemoveAllFor(_observerId).Returns(Task.FromException(_failure));
    }

    async Task Because() => _error = await Catch.Exception(_observer.Remove);

    [Fact] void should_propagate_the_original_failure() => _error.ShouldEqual(_failure);
    [Fact] void should_remain_retired() => _stateStorage.State.AlertDisposition.ShouldEqual(AlertDisposition.Retired);
    [Fact] async Task should_not_delete_the_source() => await _eventStoreNamespaceStorage.Observers.DidNotReceive().Delete(_observerId);
    [Fact] async Task should_keep_the_durable_reminder() => (await _observer.GetReminder(Observer.AlertReminderName)).ShouldNotBeNull();
}
