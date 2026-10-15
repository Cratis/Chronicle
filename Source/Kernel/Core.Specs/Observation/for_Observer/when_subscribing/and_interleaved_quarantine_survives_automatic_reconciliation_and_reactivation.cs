// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventTypes;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Observation.States;

namespace Cratis.Chronicle.Observation.for_Observer.when_subscribing;

public class and_interleaved_quarantine_survives_automatic_reconciliation_and_reactivation : given.an_observer_automatically_reconciled_during_a_probe
{
    readonly TaskCompletionSource<IEnumerable<EventTypeSchema>> _query = new(TaskCreationOptions.RunContinuationsAsynchronously);
    ObserverRunningState _runningStateBeforeReactivation;
    bool _wasQuarantinedBeforeReactivation;

    void Establish() => _eventTypesStorage.GetFor(Arg.Any<IEnumerable<EventType>>()).Returns(_ =>
    {
        _probeEntered.TrySetResult();
        return _query.Task;
    });

    async Task Because()
    {
        await QuarantineDuringProbe(ReconcileSubscription(), () => _query.SetResult([]));
        _runningStateBeforeReactivation = _stateStorage.State.RunningState;
        _wasQuarantinedBeforeReactivation = await _observer.GetCurrentState() is QuarantinedObserver;
        await Reactivate();
    }

    [Fact] void should_keep_quarantine_in_memory_before_reactivation() => _wasQuarantinedBeforeReactivation.ShouldBeTrue();
    [Fact] void should_persist_quarantine_before_reactivation() => _runningStateBeforeReactivation.ShouldEqual(ObserverRunningState.Quarantined);
    [Fact] async Task should_reactivate_in_quarantine() => (await _observer.GetCurrentState()).ShouldBeOfExactType<QuarantinedObserver>();
    [Fact] void should_keep_the_persisted_quarantine_after_reactivation() => _stateStorage.State.RunningState.ShouldEqual(ObserverRunningState.Quarantined);
}
