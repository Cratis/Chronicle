// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.Observation.States;

namespace Cratis.Chronicle.Observation.for_Observer.when_requesting_quarantine;

public class and_leaving_the_current_state_throws : given.an_observer_automatically_reconciled_during_a_probe
{
    Exception _exception;

    void Establish() => _appendedEventsQueues.Unsubscribe(Arg.Any<AppendedEventsQueueSubscription>())
        .Returns(Task.FromException(new Exception("The queue could not be unsubscribed")));

    async Task Because() => _exception = await Catch.Exception(() => _observer.RequestQuarantine());

    [Fact] void should_propagate_the_failure() => _exception.ShouldNotBeNull();
    [Fact] async Task should_clear_the_failed_pending_request() => (await _observer.IsObserverQuarantined()).ShouldBeFalse();
    [Fact] async Task should_keep_the_original_state() => (await _observer.GetCurrentState()).ShouldBeOfExactType<Observing>();
    [Fact] void should_not_persist_phantom_quarantine() => _stateStorage.State.RunningState.ShouldEqual(ObserverRunningState.Active);
}
