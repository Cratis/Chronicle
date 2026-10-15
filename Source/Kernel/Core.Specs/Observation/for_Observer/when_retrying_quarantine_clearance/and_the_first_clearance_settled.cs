// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Observation.States;
using Cratis.Orleans.Storage.Jobs;

namespace Cratis.Chronicle.Observation.for_Observer.when_retrying_quarantine_clearance;

public class and_the_first_clearance_settled : given.an_observer_with_subscription
{
    bool _owedRecoveryAfterFirstClearance;

    async Task Establish()
    {
        await _observer.TransitionTo<QuarantinedObserver>();
        await _observer.ClearObserverQuarantine();
        await _silo.TimerRegistry.FireAllAsync();
        _owedRecoveryAfterFirstClearance = _observer.OwesRecoveryAfterQuarantine();
        _appendedEventsQueues.ClearReceivedCalls();
        _jobsManager.ClearReceivedCalls();
    }

    async Task Because()
    {
        await _observer.ClearObserverQuarantine();
        await _silo.TimerRegistry.FireAllAsync();
    }

    [Fact] void should_not_owe_recovery_after_the_first_clearance() => _owedRecoveryAfterFirstClearance.ShouldBeFalse();
    [Fact] async Task should_remain_observing() => (await _observer.GetCurrentState()).ShouldBeOfExactType<Observing>();
    [Fact] void should_not_recover_again() => _jobsManager.DidNotReceive().GetJobs(Arg.Any<JobQuery>());
    [Fact] void should_not_subscribe_to_the_queue_again() => _appendedEventsQueues.DidNotReceive().Subscribe(Arg.Any<ObserverKey>(), Arg.Any<IEnumerable<EventType>>(), Arg.Any<ObserverFilters?>());
}
