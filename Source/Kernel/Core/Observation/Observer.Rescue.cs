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

    /// <summary>
    /// Recovers an observer left believing it is preparing catch-up when nothing is going to finish that preparation.
    /// <see cref="CatchUp"/> raises the preparing flag before it asks for a catch-up job, and only
    /// <see cref="RegisterCatchingUpPartitions"/> - reached exclusively when a brand new job prepares its steps -
    /// lowers it again. Every other outcome of the job request leaves the flag raised for the lifetime of the
    /// activation: the request throws, no job could be started, a stopped job was resumed, or a job was already
    /// running and had prepared its steps long before. A raised flag makes <c>Handle</c> drop every live event and
    /// makes <see cref="Observing"/> skip its missed-events check, so an observer left in it never observes anything
    /// again and, being kept alive, is never reactivated out of it.
    /// </summary>
    /// <returns>True if the preparation was cleared, false if there was nothing stranded.</returns>
    /// <remarks>
    /// The absence of a preparing or running catch-up job is what distinguishes a stranded preparation from a genuine
    /// one. The watchdog timer does not interleave with grain requests, so it can never observe the brief window
    /// inside <see cref="CatchUp"/> itself; and while a catch-up job is preparing or running, the flag is doing its
    /// job and is left alone. Clearing it inside <see cref="CatchUp"/> instead is not an option: the flag is also what
    /// stops <see cref="Observing"/> from bouncing straight back to <see cref="Routing"/> over the very gap the failed
    /// catch-up was meant to close, which would spin the state machine between the two states. Re-routing through
    /// <see cref="Routing"/> is what makes the retry the job-start path promises actually happen.
    /// </remarks>
    async Task<bool> CheckStrandedCatchupPreparation()
    {
        if (!_isPreparingCatchup || await HasRunningCatchupJob())
        {
            return false;
        }

        logger.WatchdogRescuingStrandedCatchupPreparation();
        _isPreparingCatchup = false;

        if (_subscription.IsSubscribed)
        {
            await TransitionTo<Routing>();
        }

        return true;
    }
}
