// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Observation.Jobs;
using Cratis.Chronicle.Observation.States;

namespace Cratis.Chronicle.Observation.for_Observer.given;

public class a_quarantined_observer : an_observer_with_subscription
{
    async Task Establish()
    {
        await _observer.TransitionTo<QuarantinedObserver>();
        _jobsManager.ClearReceivedCalls();
        _appendedEventsQueues.ClearReceivedCalls();
        _storageStats.ResetCounts();
    }

    protected void ShouldNotHaveResubscribed() => _appendedEventsQueues.DidNotReceive()
        .Subscribe(Arg.Any<ObserverKey>(), Arg.Any<IEnumerable<EventType>>(), Arg.Any<ObserverFilters?>());

    protected void ShouldNotHaveStartedCatchup() => _jobsManager.DidNotReceive()
        .Start<ICatchUpObserver, CatchUpObserverRequest>(Arg.Any<CatchUpObserverRequest>());

    protected void ShouldNotHaveStartedReplay() => _jobsManager.DidNotReceive()
        .Start<IReplayObserver, ReplayObserverRequest>(Arg.Any<ReplayObserverRequest>());

    protected async Task RunWatchdogTicks()
    {
        await _observer.RunWatchdogAsync();
        await _observer.RunWatchdogAsync();
        await _observer.RunWatchdogAsync();
    }
}
