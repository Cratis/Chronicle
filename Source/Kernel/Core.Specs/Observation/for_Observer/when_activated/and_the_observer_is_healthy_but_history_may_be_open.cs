// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Alerts;

namespace Cratis.Chronicle.Observation.for_Observer.when_activated;

public class and_the_observer_is_healthy_but_history_may_be_open : given.an_observer
{
    async Task Because()
    {
        await Crash();
        await _silo.TimerRegistry.FireAllAsync();
    }

    [Fact] async Task should_reconcile_the_empty_level_after_a_crash() => await _observerAlerts.Received().Reconcile(Arg.Is<ObserverAlertSnapshot>(_ => _.FailedPartitions.Count == 0 && !_.IsQuarantined));
}
