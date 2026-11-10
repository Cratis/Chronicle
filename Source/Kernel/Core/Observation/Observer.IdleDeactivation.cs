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
    /// Gets whether the watchdog has asked Orleans to deactivate this idle observer. This property is for testing purposes only.
    /// </summary>
    internal bool IsReleasedWhenIdle => _releasedWhenIdle;

    /// <summary>
    /// Gets whether the activation holds nothing that is not in storage, so it can be released without losing anything.
    /// </summary>
    /// <remarks>
    /// The grain carries <see cref="KeepAliveAttribute"/> because a subscription exists only in memory: in this
    /// activation and in the appended events queue it subscribed to. Releasing a subscribed observer would silently stop
    /// its live delivery until something subscribed it again. An observer with no subscription has no such state.
    /// Everything else it could be in the middle of - a catch-up starting, a failure being retried, an alert or a
    /// state write waiting - keeps it resident as well, so that only an observer at rest is released. A registered alert
    /// reminder would only activate it again, so the observer waits until the reminder has been given up.
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
        !HasAlertWorkPending &&
        !_alertReminderRegistered &&
        _webhookProbe is null &&
        !_armWebhookProbe &&
        !_stateTransitionInProgress &&
        !_failureStateWritePending &&
        _debouncedProgressWrites == 0 &&
        _pendingHandledCounts.Count == 0 &&
        _pendingRetryReminders.Count == 0;

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
            return true;
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

        logger.ReleasingIdleObserver(_observerId, _observerKey.EventStore, _observerKey.Namespace, _idleDeactivationAfter);
        _releasedWhenIdle = true;
        DeactivateOnIdle();
        return true;
    }
}
