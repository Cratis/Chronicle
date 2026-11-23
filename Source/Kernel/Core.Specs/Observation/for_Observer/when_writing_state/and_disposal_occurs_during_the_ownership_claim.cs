// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Storage.Observation;

namespace Cratis.Chronicle.Observation.for_Observer.when_writing_state;

public class and_disposal_occurs_during_the_ownership_claim : given.an_observer_with_controlled_persistence
{
    readonly given.held_storage_operation<bool> _claim = new();
    ulong? _epoch;
    int _attemptsBefore;

    async Task Because()
    {
        _attemptsBefore = _writes.Attempts;
        _writes.BeforeWrite = () => Task.FromException(new ObserverKeyDoesNotMatchDefinition(_observerKey, EventSequenceId.Log));
        _eventStoreNamespaceStorage.Observers.ClaimOwnership(_observerId, _observerKey.EventSequenceId).Returns(_ => _claim.Invoke());
        var writing = _observer.GrantObserverWideBeforeStart(ClaimHolderKind.ObserverCatchUp, 0UL);
        try
        {
            await _claim.WaitForStart();
            _observer.Dispose();
        }
        finally
        {
            _claim.Complete(true);
        }
        _epoch = await writing.WaitAsync(TimeSpan.FromSeconds(5), TimeProvider.System);
    }

    [Fact] void should_finish_the_issued_claim_only_once() => _eventStoreNamespaceStorage.Observers.Received(1).ClaimOwnership(Arg.Any<ObserverId>(), Arg.Any<EventSequenceId>());
    [Fact] void should_not_retry_the_state_write() => _writes.Attempts.ShouldEqual(_attemptsBefore + 1);
    [Fact] void should_not_report_a_persisted_grant() => _epoch.ShouldBeNull();
    [Fact] void should_not_save_a_timestamp() => ShouldNotSaveTimestamp();
}
