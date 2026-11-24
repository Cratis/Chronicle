// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.ReadModels.for_SinkProvenanceCache.given;

public sealed class ManualClock : TimeProvider
{
    readonly List<ManualTimer> _timers = [];
    long _timestamp;

    public override long TimestampFrequency => TimeSpan.TicksPerSecond;

    public override long GetTimestamp() => _timestamp;

    public override DateTimeOffset GetUtcNow() => DateTimeOffset.UnixEpoch.AddTicks(_timestamp);

    public override ITimer CreateTimer(TimerCallback callback, object? state, TimeSpan dueTime, TimeSpan period)
    {
        var timer = new ManualTimer(this, callback, state);
        timer.Change(dueTime, period);
        _timers.Add(timer);
        return timer;
    }

    public void Advance(TimeSpan elapsed)
    {
        _timestamp += elapsed.Ticks;
        foreach (var timer in _timers.ToArray())
        {
            timer.Tick();
        }
    }

    sealed class ManualTimer(ManualClock clock, TimerCallback callback, object? state) : ITimer
    {
        long _due;
        TimeSpan _period;
        bool _disposed;

        public bool Change(TimeSpan dueTime, TimeSpan period)
        {
            _due = dueTime == Timeout.InfiniteTimeSpan ? long.MaxValue : clock.GetTimestamp() + dueTime.Ticks;
            _period = period;
            return !_disposed;
        }

        public void Tick()
        {
            if (_disposed || clock.GetTimestamp() < _due)
            {
                return;
            }

            _due = _period == Timeout.InfiniteTimeSpan ? long.MaxValue : clock.GetTimestamp() + _period.Ticks;
            callback(state);
        }

        public void Dispose() => _disposed = true;

        public ValueTask DisposeAsync()
        {
            Dispose();
            return ValueTask.CompletedTask;
        }
    }
}
