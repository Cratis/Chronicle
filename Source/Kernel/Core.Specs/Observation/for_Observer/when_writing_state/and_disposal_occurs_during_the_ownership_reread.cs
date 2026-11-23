// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Storage.Observation;

namespace Cratis.Chronicle.Observation.for_Observer.when_writing_state;

public class and_disposal_occurs_during_the_ownership_reread : given.an_observer_with_controlled_persistence
{
    ulong? _epoch;
    int _attemptsBefore;

    async Task Because()
    {
        _attemptsBefore = _writes.Attempts;
        _writes.BeforeWrite = () => Task.FromException(new ObserverKeyDoesNotMatchDefinition(_observerKey, EventSequenceId.Log));
        _definition.SuspendNextRead = true;
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

    [Fact] void should_not_claim_ownership() => _eventStoreNamespaceStorage.Observers.DidNotReceive().ClaimOwnership(Arg.Any<ObserverId>(), Arg.Any<EventSequenceId>());
    [Fact] void should_not_retry() => _writes.Attempts.ShouldEqual(_attemptsBefore + 1);
    [Fact] void should_not_report_a_persisted_grant() => _epoch.ShouldBeNull();
}
