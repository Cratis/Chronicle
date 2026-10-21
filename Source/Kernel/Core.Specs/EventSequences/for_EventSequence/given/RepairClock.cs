// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.EventSequences.for_EventSequence.given;

public class RepairClock : TimeProvider
{
    readonly List<ClockTimer> _timers = [];
    DateTimeOffset _now = DateTimeOffset.UnixEpoch;

    public Action<TimeSpan>? TimerCreated { get; set; }

    public override DateTimeOffset GetUtcNow() => _now;

    public override ITimer CreateTimer(TimerCallback callback, object? state, TimeSpan dueTime, TimeSpan period)
    {
        var timer = new ClockTimer(this, callback, state);
        _timers.Add(timer);
        timer.Change(dueTime, period);
        TimerCreated?.Invoke(dueTime);
        return timer;
    }

    public void Advance(TimeSpan elapsed)
    {
        _now += elapsed;
        foreach (var timer in _timers.ToArray()) timer.Fire();
    }

    sealed class ClockTimer(RepairClock clock, TimerCallback callback, object? state) : ITimer
    {
        DateTimeOffset? _due;

        public bool Change(TimeSpan dueTime, TimeSpan period)
        {
            _due = dueTime == Timeout.InfiniteTimeSpan ? null : clock._now + dueTime;
            return true;
        }

        public void Fire()
        {
            if (_due is not { } due || due > clock._now) return;
            _due = null;
            callback(state);
        }

        public void Dispose() => _due = null;
        public ValueTask DisposeAsync()
        {
            _due = null;
            return ValueTask.CompletedTask;
        }
    }
}
