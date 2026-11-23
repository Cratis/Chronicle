// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Observation.for_Observer.when_changing_running_state;

public class and_a_timestamp_write_fails_after_disposal : given.an_observer_with_controlled_persistence
{
    readonly given.held_storage_operation<bool> _timestamp = new();
    readonly InvalidOperationException _failure = new("Timestamp write failed");
    Exception? _error;
    int _attemptsBefore;

    Task Because() => InGrainTurn(PerformScenario);

    async Task PerformScenario()
    {
        _attemptsBefore = _writes.Attempts;
        _eventStoreNamespaceStorage.Observers.SaveRunningStateChangedAt(_observerId, _now.AddHours(1), _observerKey.EventSequenceId).Returns(_ => _timestamp.Invoke());
        var writing = _observer.Unsubscribe();
        try
        {
            await _timestamp.WaitForStart();
            _observer.Dispose();
        }
        finally
        {
            _timestamp.Fail(_failure);
        }
        _error = await Catch.Exception(() => writing.WaitAsync(TimeSpan.FromSeconds(5), TimeProvider.System));
        await _observer.SetHandledStats(42UL);
    }

    [Fact] void should_preserve_the_real_failure() => _error.ShouldEqual(_failure);
    [Fact] void should_not_retry_the_state_write() => _writes.Attempts.ShouldEqual(_attemptsBefore + 1);
    [Fact] void should_not_retry_the_timestamp_write() => _eventStoreNamespaceStorage.Observers.Received(1).SaveRunningStateChangedAt(_observerId, _now.AddHours(1), _observerKey.EventSequenceId);
}
