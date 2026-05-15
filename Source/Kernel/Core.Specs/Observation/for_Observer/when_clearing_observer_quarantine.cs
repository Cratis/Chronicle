// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Observation.States;

namespace Cratis.Chronicle.Observation.for_Observer;

public class when_clearing_observer_quarantine : given.an_observer_with_subscription
{
    bool _isQuarantinedBefore;
    bool _isQuarantinedAfter;

    async Task Establish()
    {
        await _observer.TransitionTo<QuarantinedObserver>();
        _isQuarantinedBefore = await _observer.IsObserverQuarantined();
    }

    async Task Because()
    {
        await _observer.ClearObserverQuarantine();
        _isQuarantinedAfter = await _observer.IsObserverQuarantined();
    }

    [Fact] void should_be_quarantined_before_clearing() => _isQuarantinedBefore.ShouldBeTrue();
    [Fact] void should_not_be_quarantined_after_clearing() => _isQuarantinedAfter.ShouldBeFalse();
    [Fact] void should_transition_to_active_state() => _stateStorage.State.RunningState.ShouldEqual(ObserverRunningState.Active);
}
