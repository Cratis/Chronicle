// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation;

namespace Cratis.Chronicle.Projections.for_ProjectionsManager.when_activated;

public class and_the_wildcard_observer_is_quarantined : given.a_manager_with_a_quarantined_observer
{
    protected override bool AllEvents => true;

    Task Because() => _managerSilo.TimerRegistry.FireAllAsync();

    [Fact] async Task should_end_quarantine() => (await _observer.IsObserverQuarantined()).ShouldBeFalse();
    [Fact] async Task should_use_fresh_wildcard_subscription_without_reviving_retirement() => await _managedObserver.Received(1).SubscribeToAllEvents<IProjectionObserverSubscriber>(ObserverType.Projection, Arg.Any<SiloAddress>(), reactivateRetired: false);
}
