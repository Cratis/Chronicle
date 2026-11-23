// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Observation.for_Observer.when_changing_running_state;

public class and_storage_is_reset_during_a_history_reload : given.an_observer_with_controlled_persistence
{
    readonly given.held_storage_operation<DateTimeOffset?> _history = new();
    DateTimeOffset? _observedTimestamp;

    Task Because() => InGrainTurn(PerformScenario);

    async Task PerformScenario()
    {
        _eventStoreNamespaceStorage.Observers.GetRunningStateChangedAt(_observerId).Returns(_ => _history.Invoke());
        _definition.SuspendNextRead = true;
        var older = Subscribe();
        Task? resetting = null;
        try
        {
            await _history.WaitForStart();
            await _writes.ClearStateAsync();
            resetting = Subscribe();
            await _definition.ReadStarted.Task.WaitAsync(TimeSpan.FromSeconds(5), TimeProvider.System);
        }
        finally
        {
            _history.Complete(_now.AddDays(-3));
            _definition.ReleaseRead.TrySetResult();
        }
        await older.WaitAsync(TimeSpan.FromSeconds(5), TimeProvider.System);
        if (resetting is not null) await resetting.WaitAsync(TimeSpan.FromSeconds(5), TimeProvider.System);
        _observedTimestamp = (await _observer.GetInformation()).RunningStateChangedAt;
    }

    [Fact] void should_keep_the_reset_history_empty() => _observedTimestamp.ShouldBeNull();
    [Fact] void should_not_restore_stale_timestamp_debt() => ShouldNotSaveTimestamp();
}
