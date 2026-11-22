// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Concepts.Observation;

namespace Cratis.Chronicle.Observation.for_Observer.when_writing_state;

public class and_a_live_owner_recovers_a_mismatched_write : given.an_observer_with_controlled_persistence
{
    int _attemptsBefore;

    Task Because()
    {
        _attemptsBefore = _writes.Attempts;
        _writes.BeforeWrite = () =>
        {
            _writes.BeforeWrite = () => Task.CompletedTask;
            return Task.FromException(new ObserverKeyDoesNotMatchDefinition(_observerKey, EventSequenceId.Log));
        };
        return _observer.Unsubscribe();
    }

    [Fact] void should_claim_ownership_once() => _eventStoreNamespaceStorage.Observers.Received(1).ClaimOwnership(_observerId, _observerKey.EventSequenceId);
    [Fact] void should_retry_the_write_once() => _writes.Attempts.ShouldEqual(_attemptsBefore + 2);
    [Fact] void should_persist_the_disconnected_state() => _writes.PersistedState.RunningState.ShouldEqual(ObserverRunningState.Disconnected);
    [Fact] void should_save_the_transition_time_once() => _eventStoreNamespaceStorage.Observers.Received(1).SaveRunningStateChangedAt(_observerId, _now.AddHours(1), _observerKey.EventSequenceId);
}
