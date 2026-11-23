// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Observation.for_Observer.when_changing_running_state;

public class and_a_history_reload_finishes_after_a_newer_transition : given.an_observer_with_controlled_persistence
{
    readonly given.held_storage_operation<DateTimeOffset?> _history = new();
    DateTimeOffset? _observedTimestamp;

    Task Because() => InGrainTurn(PerformScenario);

    async Task PerformScenario()
    {
        _eventStoreNamespaceStorage.Observers.GetRunningStateChangedAt(_observerId).Returns(_ => _history.Invoke());
        _definition.SuspendNextRead = true;
        var subscribing = Subscribe();
        try
        {
            await _history.WaitForStart();
            await _observer.Unsubscribe();
            _history.Complete(_now.AddDays(-1));
            await _definition.ReadStarted.Task.WaitAsync(TimeSpan.FromSeconds(5), TimeProvider.System);
            _observedTimestamp = (await _observer.GetInformation()).RunningStateChangedAt;
        }
        finally
        {
            _history.Complete(_now.AddDays(-1));
            _definition.ReleaseRead.TrySetResult();
        }
        await subscribing.WaitAsync(TimeSpan.FromSeconds(5), TimeProvider.System);
    }

    [Fact] void should_keep_the_newer_transition_time_in_the_information_snapshot() => _observedTimestamp.ShouldEqual(_now.AddHours(1));
    [Fact] void should_not_persist_the_old_read_result() => _persistedRunningStateChangedAt.ShouldEqual(_now.AddHours(1));
}
