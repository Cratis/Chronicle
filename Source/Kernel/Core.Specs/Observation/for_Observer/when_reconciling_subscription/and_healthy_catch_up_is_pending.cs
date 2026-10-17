// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Observation;

namespace Cratis.Chronicle.Observation.for_Observer.when_reconciling_subscription;

public class and_healthy_catch_up_is_pending : given.an_observer
{
    async Task Because()
    {
        await _observer.ReconcileSubscription<NullObserverSubscriber>(ObserverType.Projection, [EventType.Unknown], SiloAddress.Zero, false);
        await _observer.CatchUp();
    }

    [Fact] async Task should_have_pending_catch_up() => (await _observer.IsPreparingCatchup()).ShouldBeTrue();
    [Fact] async Task should_report_the_subscription_as_established() => (await _observer.IsSubscriptionEstablished()).ShouldBeTrue();
    [Fact] async Task should_not_require_setup_recovery() => (await _observer.NeedsSubscriptionRecovery([EventType.Unknown])).ShouldBeFalse();
}
