// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Observation.States;

namespace Cratis.Chronicle.Observation.for_Observer.when_recovering_stalled_subscription;

public class and_setup_job_lookup_failed : given.an_observer_with_failing_setup_jobs
{
    async Task Because()
    {
        await FailSetup(() => _observer.SubscribeAdditively<NullObserverSubscriber>(ObserverType.Reactor, [EventType.Unknown], SiloAddress.Zero));
        await _observer.RecoverStalledSubscription<NullObserverSubscriber>(ObserverType.Reactor, [EventType.Unknown], SiloAddress.Zero);
    }

    [Fact] void should_surface_the_failure() => _error.ShouldBeSame(_failure);
    [Fact] void should_not_report_failed_setup_as_established() => _established.ShouldBeFalse();
    [Fact] void should_owe_recovery() => _needsRecovery.ShouldBeTrue();
    [Fact] async Task should_establish_the_subscription_on_retry() => (await _observer.IsSubscribed()).ShouldBeTrue();
    [Fact] async Task should_observe_after_retry() => (await _observer.GetCurrentState()).ShouldBeOfExactType<Observing>();
}
