// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Observation.for_Observer.when_changing_running_state;

public class and_a_current_timestamp_failure_is_retried : given.an_observer_with_controlled_persistence
{
    readonly InvalidOperationException _failure = new("Timestamp write failed");
    Exception? _error;

    Task Because() => InGrainTurn(PerformScenario);

    async Task PerformScenario()
    {
        _eventStoreNamespaceStorage.Observers.SaveRunningStateChangedAt(_observerId, _now.AddHours(1), _observerKey.EventSequenceId)
            .Returns(Task.FromException<bool>(_failure), Task.FromResult(true));
        _error = await Catch.Exception(_observer.Unsubscribe);
        await _observer.SetHandledStats(42UL);
        await _observer.SetHandledStats(43UL);
    }

    [Fact] void should_preserve_the_storage_failure() => _error.ShouldEqual(_failure);
    [Fact] void should_retry_the_unsettled_timestamp_once() => _eventStoreNamespaceStorage.Observers.Received(2).SaveRunningStateChangedAt(_observerId, _now.AddHours(1), _observerKey.EventSequenceId);
}
