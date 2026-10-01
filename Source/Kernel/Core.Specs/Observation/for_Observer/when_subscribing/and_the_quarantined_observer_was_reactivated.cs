// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Observation;

namespace Cratis.Chronicle.Observation.for_Observer.when_subscribing;

public class and_the_quarantined_observer_was_reactivated : given.a_reactivated_quarantined_observer
{
    bool _isQuarantinedBefore;

    async Task Establish() => _isQuarantinedBefore = await _observer.IsObserverQuarantined();

    async Task Because() => await _observer.Subscribe<NullObserverSubscriber>(ObserverType.Reactor, [EventType.Unknown], SiloAddress.Zero);

    [Fact] void should_be_quarantined_before_subscribing() => _isQuarantinedBefore.ShouldBeTrue();
    [Fact] void should_leave_quarantine() => _stateStorage.State.RunningState.ShouldNotEqual(ObserverRunningState.Quarantined);
    [Fact] async Task should_not_be_quarantined() => (await _observer.IsObserverQuarantined()).ShouldBeFalse();
}
