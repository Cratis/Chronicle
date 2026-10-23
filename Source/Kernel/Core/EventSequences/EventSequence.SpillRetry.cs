// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.EventSequences;

/// <summary>
/// Retries a failed spill of the appended-events queues to catch-up on a short timer.
/// </summary>
/// <remarks>
/// A failed spill means durable events may have no path to their observers: live delivery failed and the observers
/// were not told to catch up. Right after a node loss both calls can be addressed to the dead silo until the grain
/// directory catches up, so the spill is retried with a short backoff that keeps retrying at its cap until it lands,
/// rather than leaving the observers to their watchdog. A spill covers every subscriber of every queue, so any
/// successful spill - from the timer or from a later append - settles all earlier failures.
/// </remarks>
public partial class EventSequence
{
    static readonly TimeSpan[] _spillRetryDelays = [TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(2), TimeSpan.FromSeconds(5)];

    IGrainTimer? _spillRetryTimer;
    int _spillRetryAttempts;

    async Task SpillAppendedEventsQueuesToCatchup()
    {
        try
        {
            await (_appendedEventsQueues?.SpillToCatchup() ?? Task.CompletedTask);
            StopSpillRetry();
        }
        catch (Exception ex)
        {
            var delay = _spillRetryDelays[Math.Min(_spillRetryAttempts, _spillRetryDelays.Length - 1)];
            logger.FailedSpillingAppendedEventsQueuesToCatchup(_eventSequenceKey.EventStore, _eventSequenceKey.Namespace, _eventSequenceId, _spillRetryAttempts + 1, delay, ex);
            ScheduleSpillRetry(delay);
        }
    }

    void ScheduleSpillRetry(TimeSpan delay)
    {
        _spillRetryAttempts++;
        _spillRetryTimer?.Dispose();
        _spillRetryTimer = this.RegisterGrainTimer(
            RetrySpillOnTimer,
            new GrainTimerCreationOptions
            {
                DueTime = delay,
                Period = Timeout.InfiniteTimeSpan,
                Interleave = false,
                KeepAlive = true
            });
    }

    async Task RetrySpillOnTimer(CancellationToken cancellationToken)
    {
        if (_spillRetryTimer is null) return;
        await SpillAppendedEventsQueuesToCatchup();
    }

    void StopSpillRetry()
    {
        if (_spillRetryTimer is null) return;
        logger.RecoveredSpillingAppendedEventsQueuesToCatchup(_eventSequenceKey.EventStore, _eventSequenceKey.Namespace, _eventSequenceId, _spillRetryAttempts);
        _spillRetryTimer.Dispose();
        _spillRetryTimer = null;
        _spillRetryAttempts = 0;
    }

    void DisposeSpillRetry()
    {
        _spillRetryTimer?.Dispose();
        _spillRetryTimer = null;
        _spillRetryAttempts = 0;
    }
}
