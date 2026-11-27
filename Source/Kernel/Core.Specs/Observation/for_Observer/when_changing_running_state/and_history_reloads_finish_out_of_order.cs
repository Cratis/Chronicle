// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Observation.for_Observer.when_changing_running_state;

public class and_history_reloads_finish_out_of_order : given.an_observer_with_controlled_persistence
{
    readonly given.held_storage_operation<DateTimeOffset?> _olderRead = new();
    DateTimeOffset? _observedTimestamp;

    Task Because()
    {
        // Grain calls can interleave at awaits, but their continuations cannot run simultaneously.
        var grainTurns = new ConcurrentExclusiveSchedulerPair().ExclusiveScheduler;
        return Task.Factory.StartNew(PerformScenario, CancellationToken.None, TaskCreationOptions.None, grainTurns).Unwrap();
    }

    async Task PerformScenario()
    {
        var first = true;
        _eventStoreNamespaceStorage.Observers.GetRunningStateChangedAt(_observerId).Returns(_ =>
        {
            if (!first) return Task.FromResult<DateTimeOffset?>(_now.AddDays(-2));
            first = false;
            return _olderRead.Invoke();
        });
        _definition.SuspendNextRead = true;
        var older = Subscribe();
        Task? newer = null;
        try
        {
            await _olderRead.WaitForStart();
            newer = Subscribe();
            await _definition.ReadStarted.Task.WaitAsync(TimeSpan.FromSeconds(5), TimeProvider.System);
        }
        finally
        {
            _olderRead.Complete(_now.AddDays(-3));
            _definition.ReleaseRead.TrySetResult();
        }
        await older.WaitAsync(TimeSpan.FromSeconds(5), TimeProvider.System);
        if (newer is not null) await newer.WaitAsync(TimeSpan.FromSeconds(5), TimeProvider.System);
        _observedTimestamp = (await _observer.GetInformation()).RunningStateChangedAt;
    }

    [Fact] void should_keep_the_newer_reload_result() => _observedTimestamp.ShouldEqual(_now.AddDays(-2));
    [Fact] void should_not_turn_a_reload_into_a_timestamp_write() => ShouldNotSaveTimestamp();
}
