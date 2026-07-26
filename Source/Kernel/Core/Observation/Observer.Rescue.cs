// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Observation.States;

namespace Cratis.Chronicle.Observation;

public partial class Observer
{
    /// <summary>
    /// Recovers an observer that believes it is observing but is no longer on its appended-events queue. The queue
    /// drops subscriptions behind the observer's back when it spills to catch-up under back-pressure; if the spill's
    /// catch-up trigger never succeeds, the observer is left active and behind with nothing driving it forward, and
    /// nothing else reactivates it because it is kept alive. Recovery re-routes through <see cref="Routing"/>, the
    /// same transition the watchdog already uses for a missing job, which re-evaluates the gap, starts catch-up when
    /// one is needed and re-subscribes on the way back into <see cref="Observing"/>.
    /// </summary>
    /// <returns>Awaitable task.</returns>
    /// <remarks>
    /// Only reached for an observer that is subscribed, active and behind on a relevant event. Being behind on its
    /// own is normal — a live delivery in flight looks exactly the same — so the queue is asked whether it still
    /// holds the subscription, and only its answer distinguishes a stranded observer from a busy one. That question
    /// comes first because it is the one that rules out the common healthy case, leaving the job scan off the path a
    /// busy observer takes. Replaying, catching-up, failed-partition and preparing-catch-up observers are already
    /// being driven forward and are left alone, and a catch-up job that is preparing or running is never started a
    /// second time.
    /// </remarks>
    async Task CheckStrandedSubscription()
    {
        if (State.IsReplaying ||
            State.CatchingUpPartitions.Count > 0 ||
            _isPreparingCatchup ||
            Failures.HasFailedPartitions)
        {
            return;
        }

        if (await _appendedEventsQueues.IsSubscribed(_observerKey))
        {
            return;
        }

        if (await HasRunningCatchupJob())
        {
            return;
        }

        logger.WatchdogRescuingStrandedObserver(State.NextEventSequenceNumber);
        await TransitionTo<Routing>();
    }
}
