// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Observation.States;

namespace Cratis.Chronicle.Observation.for_Observer.when_subscribing;

public class and_a_late_failure_was_reported_after_unsubscribe : given.an_observer_automatically_reconciled_during_a_probe
{
    async Task Establish()
    {
        await _observer.Unsubscribe();
        await _observer.PartitionFailed(_partition, 42UL, ["Late replay failure"], string.Empty, FailureKind.Disconnected);
    }

    async Task Because() => await _observer.Subscribe<NullObserverSubscriber>(ObserverType.Reactor, [EventType.Unknown], SiloAddress.Zero);

    [Fact] async Task should_not_be_quarantined() => (await _observer.IsObserverQuarantined()).ShouldBeFalse();
    [Fact] async Task should_resume_observing() => (await _observer.GetCurrentState()).ShouldBeOfExactType<Observing>();
    [Fact] void should_persist_active_state() => _stateStorage.State.RunningState.ShouldEqual(ObserverRunningState.Active);
    [Fact] async Task should_establish_the_subscription() => (await _observer.IsSubscribed()).ShouldBeTrue();
}
