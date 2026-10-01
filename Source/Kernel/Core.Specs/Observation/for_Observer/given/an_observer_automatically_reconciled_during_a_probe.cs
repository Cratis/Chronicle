// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Observation.EventStoreSubscriptions;
using Cratis.Chronicle.Observation.Jobs;
using Cratis.Orleans.Jobs;

namespace Cratis.Chronicle.Observation.for_Observer.given;

public class an_observer_automatically_reconciled_during_a_probe : an_observer_quarantined_during_a_probe
{
    protected Task ReconcileSubscription() => _observer.Subscribe<IEventStoreSubscriptionObserverSubscriber>(
        ObserverType.External,
        [EventType.Unknown],
        SiloAddress.Zero,
        "target",
        automatic: true);

    protected void ShouldNotSubscribeToQueue() => _appendedEventsQueues.DidNotReceive()
        .Subscribe(Arg.Any<ObserverKey>(), Arg.Any<IEnumerable<EventType>>(), Arg.Any<ObserverFilters?>());

    protected void ShouldNotStartReplay() => _jobsManager.DidNotReceive()
        .Start<IReplayObserver, ReplayObserverRequest>(Arg.Any<ReplayObserverRequest>());

    protected void ShouldNotStartCatchup() => _jobsManager.DidNotReceive()
        .Start<ICatchUpObserver, CatchUpObserverRequest>(Arg.Any<CatchUpObserverRequest>());

    protected void ShouldNotResumeJobs() => _jobsManager.DidNotReceive().Resume(Arg.Any<JobId>());
}
