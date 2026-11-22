// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Registrations;

/// <summary>
/// A <see cref="TimeProvider"/> whose time, and the timers created from it, only move when a specification advances it.
/// </summary>
public class ManualTimeProvider : TimeProvider
{
    readonly List<ManualTimer> _timers = [];
    DateTimeOffset _now = new(2026, 11, 21, 12, 0, 0, TimeSpan.Zero);

    /// <inheritdoc/>
    public override DateTimeOffset GetUtcNow() => _now;

    /// <inheritdoc/>
    public override ITimer CreateTimer(TimerCallback callback, object? state, TimeSpan dueTime, TimeSpan period)
    {
        var timer = new ManualTimer(callback, state, _now + dueTime);
        lock (_timers)
        {
            _timers.Add(timer);
        }

        return timer;
    }

    /// <summary>
    /// Moves time forward and fires every timer that has become due.
    /// </summary>
    /// <param name="by">How far to move it.</param>
    public void Advance(TimeSpan by)
    {
        _now += by;
        ManualTimer[] due;
        lock (_timers)
        {
            due = [.. _timers.Where(timer => !timer.Disposed && timer.Due <= _now)];
            _timers.RemoveAll(timer => due.Contains(timer));
        }

        foreach (var timer in due)
        {
            timer.Fire();
        }
    }

    sealed class ManualTimer(TimerCallback callback, object? state, DateTimeOffset due) : ITimer
    {
        public DateTimeOffset Due { get; } = due;

        public bool Disposed { get; private set; }

        public bool Change(TimeSpan dueTime, TimeSpan period) => false;

        public void Dispose() => Disposed = true;

        public ValueTask DisposeAsync()
        {
            Dispose();
            return ValueTask.CompletedTask;
        }

        public void Fire() => callback(state);
    }
}
