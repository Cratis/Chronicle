// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Observation.States;

namespace Cratis.Chronicle.Observation.for_Observer.when_reconciling_subscription;

public class and_job_lookup_fails_after_installation : given.an_observer_with_failing_setup_jobs
{
    Task Subscribe() => _observer.ReconcileSubscription<NullObserverSubscriber>(ObserverType.Projection, [EventType.Unknown], SiloAddress.Zero, false);

    async Task Because()
    {
        await FailSetup(Subscribe);
        await Subscribe();
    }

    [Fact] void should_surface_the_failure() => _error.ShouldBeSame(_failure);
    [Fact] void should_have_installed_the_subscription_before_failure() => _installed.ShouldBeTrue();
    [Fact] void should_not_report_failed_setup_as_established() => _established.ShouldBeFalse();
    [Fact] void should_owe_recovery() => _needsRecovery.ShouldBeTrue();
    [Fact] async Task should_establish_the_subscription_on_the_next_pass() => (await _observer.IsSubscriptionEstablished()).ShouldBeTrue();
    [Fact] async Task should_observe_after_retry() => (await _observer.GetCurrentState()).ShouldBeOfExactType<Observing>();
}
