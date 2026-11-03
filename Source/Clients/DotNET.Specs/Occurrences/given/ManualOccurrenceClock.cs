// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Threading.Channels;

namespace Cratis.Chronicle.Occurrences.given;

public class ManualOccurrenceClock : TimeProvider
{
    readonly object _gate = new();
    readonly List<ManualTimer> _timers = [];
    readonly Channel<(ManualTimer Timer, TimeSpan DueTime)> _scheduled = Channel.CreateUnbounded<(ManualTimer, TimeSpan)>();
    long _timestamp;
    DateTimeOffset _utcNow = DateTimeOffset.UnixEpoch;

    public TaskCompletionSource<ManualTimer> Created { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

    public override long TimestampFrequency => TimeSpan.TicksPerSecond;

    public override long GetTimestamp()
    {
        lock (_gate) return _timestamp;
    }

    public override DateTimeOffset GetUtcNow()
    {
        lock (_gate) return _utcNow;
    }

    public override ITimer CreateTimer(TimerCallback callback, object state, TimeSpan dueTime, TimeSpan period)
    {
        var timer = new ManualTimer(this, callback, state);
        lock (_gate) _timers.Add(timer);
        timer.Change(dueTime, period);
        Created.TrySetResult(timer);
        return timer;
    }

    public async Task<ManualTimer> WaitForTimer(TimeSpan dueTime, CancellationToken cancellationToken = default)
    {
        using var guard = new CancellationTokenSource(TimeSpan.FromSeconds(5), TimeProvider.System);
        using var linked = CancellationTokenSource.CreateLinkedTokenSource(guard.Token, cancellationToken);
        while (true)
        {
            var scheduled = await _scheduled.Reader.ReadAsync(linked.Token);
            if (scheduled.DueTime == dueTime && !scheduled.Timer.Disposed) return scheduled.Timer;
        }
    }

    public void Advance(TimeSpan elapsed, bool deliverCallbacks = true)
    {
        lock (_gate)
        {
            _timestamp += elapsed.Ticks;
            _utcNow += elapsed;
        }
        if (!deliverCallbacks) return;
        foreach (var callback in CaptureDueCallbacks()) callback();
    }

    public void AdvanceUtc(TimeSpan elapsed)
    {
        lock (_gate) _utcNow += elapsed;
    }

    public Action[] CaptureDueCallbacks()
    {
        lock (_gate)
        {
            return _timers.Where(timer => !timer.Disposed && timer.Next <= _timestamp)
                .Select(timer =>
                {
                    timer.Next = timer.Period > TimeSpan.Zero
                        ? timer.Next + ((((_timestamp - timer.Next) / timer.Period.Ticks) + 1) * timer.Period.Ticks)
                        : long.MaxValue;
                    return timer.CaptureCallback();
                }).ToArray();
        }
    }

    public sealed class ManualTimer(ManualOccurrenceClock clock, TimerCallback callback, object state) : ITimer
    {
        public TimeSpan DueTime { get; private set; }

        public TimeSpan Period { get; private set; }

        public long Next { get; internal set; } = long.MaxValue;

        public bool Disposed { get; private set; }

        public void Tick()
        {
            Action invoke;
            lock (clock._gate)
            {
                if (Disposed) return;
                invoke = CaptureCallback();
            }
            invoke();
        }

        /// <summary>
        /// Captures a callback that may already be queued when its timer is changed or disposed.
        /// </summary>
        /// <returns>The queued callback.</returns>
        public Action CaptureCallback() => () => callback(state);

        public bool Change(TimeSpan dueTime, TimeSpan period)
        {
            lock (clock._gate)
            {
                if (Disposed) return false;
                DueTime = dueTime;
                Period = period;
                Next = dueTime == Timeout.InfiniteTimeSpan ? long.MaxValue : clock._timestamp + dueTime.Ticks;
            }
            clock._scheduled.Writer.TryWrite((this, dueTime));
            return true;
        }

        public void Dispose() => Stop();

        public ValueTask DisposeAsync()
        {
            Stop();
            return ValueTask.CompletedTask;
        }

        void Stop()
        {
            lock (clock._gate) Disposed = true;
        }
    }
}
