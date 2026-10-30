// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Observation.Jobs;

namespace Cratis.Chronicle.Observation.for_Observer.when_restoring_live_subscription_after_zero_work.given;

public class an_observer_rejoining_during_backoff : when_watchdog_runs.given.an_observer_with_zero_key_catch_up_jobs
{
    protected readonly List<CatchUpObserverRequest> _retries = [];

    async Task Establish()
    {
        await SettleAfterZeroKeyJobs();
        _preparedJobs.Count.ShouldEqual(2);
        _queued = false;
        await _observer.RunWatchdogAsync();
        _queued = false;
    }

    protected void RecordRetries() => _jobsManager
        .Start<ICatchUpObserver, CatchUpObserverRequest>(Arg.Any<CatchUpObserverRequest>())
        .Returns(call =>
        {
            var request = call.Arg<CatchUpObserverRequest>();
            _retries.Add(request);
            return StartZeroKeyJob(request);
        });

    protected async Task DeliverLive(EventSequenceNumber sequenceNumber)
    {
        _subscriber.OnNext(Arg.Any<Key>(), Arg.Any<IEnumerable<AppendedEvent>>(), Arg.Any<ObserverSubscriberContext>())
            .Returns(ObserverSubscriberResult.Ok(sequenceNumber));
        await _observer.Handle("live", [AppendedEvent.EmptyWithEventTypeAndEventSequenceNumber(event_type, sequenceNumber)]);
    }
}
