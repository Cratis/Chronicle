// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Storage.Observation;

namespace Cratis.Chronicle.Observation.for_Observer.when_writing_state;

public class and_disposal_occurs_during_the_second_ownership_reread : given.an_observer_with_controlled_persistence
{
    ulong? _epoch;
    int _attemptsBefore;

    Task Because() => InGrainTurn(PerformScenario);

    async Task PerformScenario()
    {
        _attemptsBefore = _writes.Attempts;
        _writes.BeforeWrite = () =>
        {
            if (_writes.Attempts == _attemptsBefore + 2) _definition.SuspendNextRead = true;
            return Task.FromException(new ObserverKeyDoesNotMatchDefinition(_observerKey, EventSequenceId.Log));
        };
        var writing = _observer.GrantObserverWideBeforeStart(ClaimHolderKind.ObserverCatchUp, 0UL);
        try
        {
            await _definition.ReadStarted.Task.WaitAsync(TimeSpan.FromSeconds(5), TimeProvider.System);
            _observer.Dispose();
        }
        finally
        {
            _definition.ReleaseRead.TrySetResult();
        }
        _epoch = await writing.WaitAsync(TimeSpan.FromSeconds(5), TimeProvider.System);
    }

    [Fact] void should_not_attempt_another_retry() => _writes.Attempts.ShouldEqual(_attemptsBefore + 2);
    [Fact] void should_not_report_a_persisted_grant() => _epoch.ShouldBeNull();
    [Fact] void should_not_save_a_timestamp() => ShouldNotSaveTimestamp();
    [Fact] void should_not_reclaim_ownership_again() => _eventStoreNamespaceStorage.Observers.Received(1).ClaimOwnership(_observerId, _observerKey.EventSequenceId);
}
