// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Observation.States;

namespace Cratis.Chronicle.Observation.for_Observer.when_subscribing;

public class and_quarantine_begins_during_automatic_reconciliation_tail_query : given.an_observer_automatically_reconciled_during_a_probe
{
    readonly TaskCompletionSource<EventSequenceNumber> _query = new(TaskCreationOptions.RunContinuationsAsynchronously);

    void Establish() => _eventSequence.GetTailSequenceNumber().Returns(_ =>
    {
        _probeEntered.TrySetResult();
        return _query.Task;
    });

    async Task Because() => await QuarantineDuringProbe(ReconcileSubscription(), () => _query.SetResult(42UL));

    [Fact] async Task should_keep_the_quarantined_state() => (await _observer.GetCurrentState()).ShouldBeOfExactType<QuarantinedObserver>();
    [Fact] void should_keep_the_persisted_quarantine() => _stateStorage.State.RunningState.ShouldEqual(ObserverRunningState.Quarantined);
    [Fact] void should_not_resume_jobs() => ShouldNotResumeJobs();
    [Fact] void should_not_start_replay() => ShouldNotStartReplay();
    [Fact] void should_not_start_catchup() => ShouldNotStartCatchup();
    [Fact] void should_not_resubscribe_to_the_queue() => ShouldNotSubscribeToQueue();
}
