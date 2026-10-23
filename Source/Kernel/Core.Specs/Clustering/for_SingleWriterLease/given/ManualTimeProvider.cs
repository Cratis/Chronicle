// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.SingleWriter.for_SingleWriterLease.given;

public sealed class ManualTimeProvider : TimeProvider
{
    readonly List<ManualTimer> _timers = [];
    readonly object _gate = new();
    long _ticks;
    TaskCompletionSource? _nextTimer;

    public override long TimestampFrequency => TimeSpan.TicksPerSecond;
    public override long GetTimestamp() => _ticks;
    public override DateTimeOffset GetUtcNow() => DateTimeOffset.UnixEpoch.AddTicks(_ticks);

    public Task WhenNextTimer()
    {
        lock (_gate)
        {
            _nextTimer = new(TaskCreationOptions.RunContinuationsAsynchronously);
            return _nextTimer.Task.WaitAsync(TimeSpan.FromSeconds(5), TimeProvider.System);
        }
    }

    public override ITimer CreateTimer(TimerCallback callback, object? state, TimeSpan dueTime, TimeSpan period)
    {
        lock (_gate)
        {
            var timer = new ManualTimer(_gate, GetTimestamp, callback, state);
            _timers.Add(timer);
            timer.Change(dueTime, period);
            _nextTimer?.TrySetResult();
            _nextTimer = null;
            return timer;
        }
    }

    public void Advance(TimeSpan by)
    {
        lock (_gate)
        {
            var until = _ticks + by.Ticks;
            while (_timers.Where(timer => timer.Due <= until).MinBy(timer => timer.Due) is { } next)
            {
                _ticks = next.Due;
                next.Due = long.MaxValue;
                next.Callback(next.State);
            }

            _ticks = until;
        }
    }
}
