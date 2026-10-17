// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Observation;

namespace Cratis.Chronicle.Projections.for_ProjectionsManager.when_activated;

public class and_the_observer_is_quarantined : given.a_manager_with_a_quarantined_observer
{
    Task Because() => _managerSilo.TimerRegistry.FireAllAsync();

    [Fact] async Task should_end_quarantine() => (await _observer.IsObserverQuarantined()).ShouldBeFalse();
    [Fact] async Task should_use_fresh_subscription_without_reviving_retirement() => await _managedObserver.Received(1).Subscribe<IProjectionObserverSubscriber>(ObserverType.Projection, Arg.Any<IEnumerable<EventType>>(), Arg.Any<SiloAddress>(), reactivateRetired: false);
}
