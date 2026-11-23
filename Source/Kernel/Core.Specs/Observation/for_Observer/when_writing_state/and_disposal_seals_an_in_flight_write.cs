// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Observation;

namespace Cratis.Chronicle.Observation.for_Observer.when_writing_state;

public class and_disposal_seals_an_in_flight_write : given.an_observer_with_controlled_persistence
{
    readonly given.held_storage_operation<bool> _write = new();
    ulong? _epoch;
    Exception? _error;
    int _attemptsBefore;
    bool _stillJoined;
    bool _queuedStillJoined;

    Task Because() => InGrainTurn(PerformScenario);

    async Task PerformScenario()
    {
        _attemptsBefore = _writes.Attempts;
        _writes.BeforeWrite = _write.Invoke;
        var writing = _observer.Unsubscribe();
        Task<ulong?>? queued = null;
        try
        {
            await _write.WaitForStart();
            queued = _observer.GrantObserverWideBeforeStart(ClaimHolderKind.ObserverCatchUp, 0UL);
            _observer.Dispose();
            _stillJoined = !writing.IsCompleted;
            _queuedStillJoined = !queued.IsCompleted;
        }
        finally
        {
            _write.Complete(true);
        }
        _error = await Catch.Exception(() => writing.WaitAsync(TimeSpan.FromSeconds(5), TimeProvider.System));
        if (queued is not null) _epoch = await queued.WaitAsync(TimeSpan.FromSeconds(5), TimeProvider.System);
    }

    [Fact] void should_not_abandon_the_issued_write() => _stillJoined.ShouldBeTrue();
    [Fact] void should_keep_the_waiter_joined_until_the_gate_is_released() => _queuedStillJoined.ShouldBeTrue();
    [Fact] void should_let_the_issued_write_complete() => _writes.Stats.Writes.ShouldEqual(1);
    [Fact] void should_skip_the_timestamp_follow_up() => ShouldNotSaveTimestamp();
    [Fact] void should_skip_the_queued_write() => _writes.Attempts.ShouldEqual(_attemptsBefore + 1);
    [Fact] void should_not_count_the_stale_write_as_a_persisted_grant() => _epoch.ShouldBeNull();
    [Fact] void should_release_the_gate_without_disposal_errors() => _error.ShouldBeNull();
}
