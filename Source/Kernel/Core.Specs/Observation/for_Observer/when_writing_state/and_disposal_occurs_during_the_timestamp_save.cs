// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Observation;

namespace Cratis.Chronicle.Observation.for_Observer.when_writing_state;

public class and_disposal_occurs_during_the_timestamp_save : given.an_observer_with_controlled_persistence
{
    readonly given.held_storage_operation<bool> _timestamp = new();
    ulong? _epoch;
    int _attemptsBefore;

    async Task Because()
    {
        _attemptsBefore = _writes.Attempts;
        _eventStoreNamespaceStorage.Observers.SaveRunningStateChangedAt(_observerId, _now.AddHours(1), _observerKey.EventSequenceId).Returns(_ => _timestamp.Invoke());
        var writing = _observer.Unsubscribe();
        Task<ulong?>? queued = null;
        try
        {
            await _timestamp.WaitForStart();
            queued = _observer.GrantObserverWideBeforeStart(ClaimHolderKind.ObserverCatchUp, 0UL);
            _observer.Dispose();
        }
        finally
        {
            _timestamp.Complete(true);
        }
        await writing.WaitAsync(TimeSpan.FromSeconds(5), TimeProvider.System);
        if (queued is not null) _epoch = await queued.WaitAsync(TimeSpan.FromSeconds(5), TimeProvider.System);
    }

    [Fact] void should_complete_the_already_issued_state_write() => _writes.Stats.Writes.ShouldEqual(1);
    [Fact] void should_not_start_the_queued_write() => _writes.Attempts.ShouldEqual(_attemptsBefore + 1);
    [Fact] void should_not_count_the_stale_write_as_a_persisted_grant() => _epoch.ShouldBeNull();
}
