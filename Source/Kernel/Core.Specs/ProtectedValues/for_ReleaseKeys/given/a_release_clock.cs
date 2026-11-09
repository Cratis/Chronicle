// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.ProtectedValues.for_ReleaseKeys.given;

public class a_release_clock : TimeProvider
{
    readonly List<ClockTimer> _timers = [];
    long _timestamp;
    DateTimeOffset _wall = DateTimeOffset.UnixEpoch;

    public override long TimestampFrequency => TimeSpan.TicksPerSecond;
    public override long GetTimestamp() => _timestamp;
    public override DateTimeOffset GetUtcNow() => _wall;

    public void Advance(TimeSpan duration)
    {
        _timestamp += duration.Ticks;
        _wall += duration;
        foreach (var timer in _timers.ToArray()) timer.Fire();
    }

    public void JumpWall(TimeSpan duration) => _wall += duration;

    public override ITimer CreateTimer(TimerCallback callback, object? state, TimeSpan dueTime, TimeSpan period)
    {
        var timer = new ClockTimer(this, callback, state);
        _timers.Add(timer);
        timer.Change(dueTime, period);
        return timer;
    }

    sealed class ClockTimer(a_release_clock clock, TimerCallback callback, object? state) : ITimer
    {
        long _due = long.MaxValue;
        TimeSpan _period;

        public bool Change(TimeSpan dueTime, TimeSpan period)
        {
            _due = dueTime == Timeout.InfiniteTimeSpan ? long.MaxValue : clock._timestamp + dueTime.Ticks;
            _period = period;
            return true;
        }

        public void Fire()
        {
            if (clock._timestamp < _due) return;
            _due = _period > TimeSpan.Zero ? clock._timestamp + _period.Ticks : long.MaxValue;
            callback(state);
        }

        public void Dispose() => clock._timers.Remove(this);

        public ValueTask DisposeAsync()
        {
            clock._timers.Remove(this);
            return ValueTask.CompletedTask;
        }
    }
}
