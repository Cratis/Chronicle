// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Naming.for_ResourceNameAudit.given;

/// <summary>
/// Represents a <see cref="TimeProvider"/> whose time only moves when it is advanced.
/// </summary>
public sealed class ManualTimeProvider : TimeProvider
{
    readonly List<ManualTimer> _timers = [];
    DateTimeOffset _now = new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);

    /// <inheritdoc/>
    public override DateTimeOffset GetUtcNow() => _now;

    /// <inheritdoc/>
    public override ITimer CreateTimer(TimerCallback callback, object? state, TimeSpan dueTime, TimeSpan period)
    {
        var timer = new ManualTimer(this, callback, state, dueTime);
        lock (_timers)
        {
            _timers.Add(timer);
        }

        return timer;
    }

    /// <summary>
    /// Move the time forward and fire the timers that have become due.
    /// </summary>
    /// <param name="amount">How far to move.</param>
    public void Advance(TimeSpan amount)
    {
        ManualTimer[] due;
        lock (_timers)
        {
            _now += amount;
            due = [.. _timers.Where(_ => _.IsDue(_now))];
            foreach (var timer in due)
            {
                _timers.Remove(timer);
            }
        }

        foreach (var timer in due)
        {
            timer.Fire();
        }
    }

    void Remove(ManualTimer timer)
    {
        lock (_timers)
        {
            _timers.Remove(timer);
        }
    }

    sealed class ManualTimer(ManualTimeProvider owner, TimerCallback callback, object? state, TimeSpan dueTime) : ITimer
    {
        readonly DateTimeOffset _due = owner._now + dueTime;
        bool _disposed;

        public bool Change(TimeSpan dueTime, TimeSpan period) => false;

        public void Dispose() => Release();

        public ValueTask DisposeAsync()
        {
            Release();
            return ValueTask.CompletedTask;
        }

        public bool IsDue(DateTimeOffset now) => !_disposed && dueTime != Timeout.InfiniteTimeSpan && now >= _due;

        public void Fire() => callback(state);

        void Release()
        {
            _disposed = true;
            owner.Remove(this);
        }
    }
}
