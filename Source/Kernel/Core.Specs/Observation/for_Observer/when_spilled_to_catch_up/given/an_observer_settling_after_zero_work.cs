// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.Observation.Jobs;
using Cratis.Chronicle.Observation.States;
using Cratis.Orleans.Jobs;

namespace Cratis.Chronicle.Observation.for_Observer.when_spilled_to_catch_up.given;

public class an_observer_settling_after_zero_work : for_Observer.given.an_observer
{
    protected bool _queued;
    protected JobId _jobId = JobId.New();

    async Task Establish()
    {
        _appendedEventsQueues.IsSubscribed(_observerKey).Returns(_ => _queued);
        _appendedEventsQueues.Subscribe(_observerKey, Arg.Any<IEnumerable<EventType>>(), Arg.Any<ObserverFilters?>())
            .Returns(_ =>
            {
                _queued = true;
                return new AppendedEventsQueueSubscription(_observerKey, 0);
            });
        _appendedEventsQueues.When(_ => _.Unsubscribe(Arg.Any<AppendedEventsQueueSubscription>())).Do(_ => _queued = false);
        _jobsManager.Start<ICatchUpObserver, CatchUpObserverRequest>(Arg.Any<CatchUpObserverRequest>()).Returns(_jobId);
        _subscriber.OnNext(Arg.Any<Key>(), Arg.Any<IEnumerable<AppendedEvent>>(), Arg.Any<ObserverSubscriberContext>())
            .Returns(ObserverSubscriberResult.Ok(1UL));

        await _observer.Subscribe<IObserverSubscriber>(ObserverType.Reactor, [EventType.Unknown], SiloAddress.Zero);
        await _observer.CaughtUp(JobId.New(), 0UL);

        // The first zero-work lap routes normally. The next one settles directly into Observing with backoff.
        await _observer.CaughtUp(JobId.New(), 0UL);
        (await _observer.GetCurrentState()).ShouldBeOfExactType<Observing>();
        _queued.ShouldBeTrue();
        await _observer.CatchUp();
        _appendedEventsQueues.ClearReceivedCalls();
        _jobsManager.ClearReceivedCalls();
    }
}
