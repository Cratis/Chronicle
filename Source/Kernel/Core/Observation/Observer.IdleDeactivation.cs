// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation;

namespace Cratis.Chronicle.Observation;

public partial class Observer
{
    TimeSpan _idleDeactivationAfter;
    DateTimeOffset? _idleSince;
    bool _releasedWhenIdle;

    /// <summary>
    /// Gets whether the watchdog released this activation as idle and it is still idle, so deactivating it changes nothing.
    /// </summary>
    bool IsReleasedAsIdle => _releasedWhenIdle && IsIdle;

    /// <summary>
    /// Gets whether the activation holds nothing that is not in storage, so it can be released without losing anything.
    /// </summary>
    /// <remarks>
    /// The grain carries <see cref="KeepAliveAttribute"/> because a subscription exists only in memory: in this
    /// activation and in the appended events queue it subscribed to. Releasing a subscribed observer would silently stop
    /// its live delivery until something subscribed it again. An observer with no subscription has no such state.
    /// Everything else it could be in the middle of - a catch-up starting, a failure being retried, an alert or a
    /// state write waiting, a request kept for a later turn or a later subscription - keeps it resident as well, so that
    /// only an observer at rest is released. Each of those is listed here rather than inferred from recent calls, because
    /// some of them, such as a retained start-at-tail request, outlive any idle period. A registered alert reminder would
    /// only activate it again, so the observer waits until the reminder has been given up. An activation with no stored
    /// observer behind it - a probe, or a key under another event sequence than the definition's - writes no state and
    /// has no alerts to report, so only the work it could still have in memory keeps it.
    /// </remarks>
    bool IsIdle =>
        !_removed &&
        !_subscription.IsSubscribed &&
        (!_observerExists || State.RunningState == ObserverRunningState.Disconnected) &&
        !IsQuarantined &&
        !State.IsReplaying &&
        !State.ReplayingPartitions.Any() &&
        !State.CatchingUpPartitions.Any() &&
        _pendingCatchUpAcquisition is null &&
        !Failures.HasFailedPartitions &&
        (!_observerExists || (IsAlertWorkSettled && !_runningStateChangedAtPending)) &&
        _webhookProbe is null &&
        !_armWebhookProbe &&
        !_stateTransitionInProgress &&
        !_failureStateWritePending &&
        _debouncedProgressWrites == 0 &&
        _pendingHandledCounts.Count == 0 &&
        _pendingRetryReminders.Count == 0 &&
        !_startAtTailWhenSubscribed &&
        !_partitionRecoveryTurnScheduled &&
        !_recoveryOfAllFailedPartitionsRequested &&
        !_retryJobSweepRequested;

    /// <summary>
    /// Gets whether nothing about the observer's alerts is waiting: no report pending or running, and no reminder left
    /// to wake the observer.
    /// </summary>
    /// <remarks>
    /// A scheduled report turn is not checked on its own: it reports only while a reconciliation is pending, which is.
    /// </remarks>
    bool IsAlertWorkSettled =>
        !HasAlertWorkPending &&
        !_alertReconciliationPending &&
        !_alertReportInProgress &&
        !_alertReminderRegistered;

    void ConfigureIdleDeactivation(int idleDeactivationAfterSeconds) =>
        _idleDeactivationAfter = TimeSpan.FromSeconds(Math.Max(idleDeactivationAfterSeconds, 0));

    /// <summary>
    /// Releases the activation once the watchdog has seen it idle for the configured period.
    /// </summary>
    /// <returns>True when the activation is being released; otherwise false.</returns>
    /// <remarks>
    /// The period starts at the first tick that finds the observer idle and starts over whenever a tick finds it busy,
    /// so an observer that only passes through an idle moment is not released. A released observer is activated again
    /// from storage by the next call, the same way it is after a silo restart.
    /// </remarks>
    bool ReleaseWhenIdle()
    {
        if (_releasedWhenIdle)
        {
            // Orleans deactivates the activation as soon as it is idle, so a tick should not get here. Should one, the
            // activation either asks again or, if work arrived in the meantime, runs its watchdog as before.
            if (IsIdle)
            {
                DeactivateOnIdle();
                return true;
            }

            logger.IdleObserverBusyAgain();
            _releasedWhenIdle = false;
            _idleSince = null;
            return false;
        }

        if (_idleDeactivationAfter <= TimeSpan.Zero || !IsIdle)
        {
            _idleSince = null;
            return false;
        }

        var now = _clock.GetUtcNow();
        _idleSince ??= now;
        if (now - _idleSince.Value < _idleDeactivationAfter)
        {
            return false;
        }

        logger.ReleasingIdleObserver(_idleDeactivationAfter);
        _releasedWhenIdle = true;
        DeactivateOnIdle();
        return true;
    }
}
