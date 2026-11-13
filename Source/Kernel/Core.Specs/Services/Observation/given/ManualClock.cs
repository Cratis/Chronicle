// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Threading.Channels;

namespace Cratis.Chronicle.Services.Observation.given;

/// <summary>
/// A <see cref="TimeProvider"/> whose time only moves when told to, and which tells when something starts waiting on it.
/// </summary>
/// <remarks>
/// Every timer created - which is what a wait on the clock creates - is reported through <see cref="NextWait"/>, so a
/// specification knows the code is waiting, and for how long, before it moves the clock. Nothing waits on real time.
/// </remarks>
public sealed class ManualClock : TimeProvider
{
    /// <summary>
    /// How long a specification waits for something that should happen at once before it gives up.
    /// </summary>
    public static readonly TimeSpan Patience = TimeSpan.FromSeconds(10);

    /// <summary>
    /// Waits for a task that should complete at once, giving up after <see cref="Patience"/> of real time.
    /// </summary>
    /// <param name="task">The task.</param>
    /// <returns>The awaitable task.</returns>
    public static Task Within(Task task) => task.WaitAsync(Patience, TimeProvider.System);

    readonly Lock _lock = new();
    readonly List<ManualTimer> _timers = [];
    readonly Channel<TimeSpan> _waits = Channel.CreateUnbounded<TimeSpan>();
    DateTimeOffset _now = new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);

    /// <inheritdoc/>
    public override DateTimeOffset GetUtcNow()
    {
        lock (_lock)
        {
            return _now;
        }
    }

    /// <inheritdoc/>
    public override ITimer CreateTimer(TimerCallback callback, object? state, TimeSpan dueTime, TimeSpan period)
    {
        var timer = new ManualTimer(this, callback, state);
        timer.Change(dueTime, period);
        _waits.Writer.TryWrite(dueTime);
        return timer;
    }

    /// <summary>
    /// Waits for the next wait to start on the clock.
    /// </summary>
    /// <returns>How long it waits.</returns>
    public async Task<TimeSpan> NextWait() => await _waits.Reader.ReadAsync().AsTask().WaitAsync(Patience, TimeProvider.System);

    /// <summary>
    /// Waits for the next wait to start on the clock and lets it elapse.
    /// </summary>
    /// <returns>How long it waited.</returns>
    public async Task<TimeSpan> Elapse()
    {
        var wait = await NextWait();
        Advance(wait);
        return wait;
    }

    /// <summary>
    /// Moves the clock forward, firing the timers that come due on the way.
    /// </summary>
    /// <param name="duration">How far.</param>
    public void Advance(TimeSpan duration)
    {
        DateTimeOffset until;
        lock (_lock)
        {
            until = _now + duration;
        }

        while (true)
        {
            ManualTimer? due;
            lock (_lock)
            {
                due = _timers.Where(timer => timer.DueAt <= until).MinBy(timer => timer.DueAt);
                if (due is null)
                {
                    _now = until;
                    return;
                }

                _now = due.DueAt;
                _timers.Remove(due);
            }

            due.Fire();
        }
    }

    void Schedule(ManualTimer timer, DateTimeOffset? dueAt)
    {
        lock (_lock)
        {
            _timers.Remove(timer);
            if (dueAt is { } at)
            {
                timer.DueAt = at;
                _timers.Add(timer);
            }
        }
    }

    sealed class ManualTimer(ManualClock clock, TimerCallback callback, object? state) : ITimer
    {
        TimeSpan _period = Timeout.InfiniteTimeSpan;

        public DateTimeOffset DueAt { get; set; }

        public bool Change(TimeSpan dueTime, TimeSpan period)
        {
            _period = period;
            clock.Schedule(this, dueTime == Timeout.InfiniteTimeSpan ? null : clock.GetUtcNow() + dueTime);
            return true;
        }

        public void Fire()
        {
            if (_period != Timeout.InfiniteTimeSpan && _period > TimeSpan.Zero)
            {
                clock.Schedule(this, DueAt + _period);
            }

            callback(state);
        }

        public void Dispose() => clock.Schedule(this, null);

        public ValueTask DisposeAsync()
        {
            clock.Schedule(this, null);
            return ValueTask.CompletedTask;
        }
    }
}
