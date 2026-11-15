// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Threading.Channels;

namespace Cratis.Chronicle.Federation.given;

/// <summary>
/// A time provider whose timers only fire when a spec advances the clock, so a delay is awaited as a fact and never as a duration.
/// </summary>
public class TimerClock : TimeProvider
{
    readonly object _gate = new();
    readonly List<ClockTimer> _timers = [];
    readonly Channel<bool> _scheduled = Channel.CreateUnbounded<bool>();
    long _ticks;

    /// <inheritdoc/>
    public override long TimestampFrequency => TimeSpan.TicksPerSecond;

    /// <inheritdoc/>
    public override long GetTimestamp() => Interlocked.Read(ref _ticks);

    /// <inheritdoc/>
    public override DateTimeOffset GetUtcNow() => DateTimeOffset.UnixEpoch.AddTicks(GetTimestamp());

    /// <inheritdoc/>
    public override ITimer CreateTimer(TimerCallback callback, object? state, TimeSpan dueTime, TimeSpan period)
    {
        var timer = new ClockTimer(this, callback, state, dueTime);
        lock (_gate)
        {
            _timers.Add(timer);
        }

        _scheduled.Writer.TryWrite(true);
        return timer;
    }

    /// <summary>
    /// Gets how many timers are pending.
    /// </summary>
    public int PendingTimers
    {
        get
        {
            lock (_gate)
            {
                return _timers.Count(_ => !_.IsDisposed);
            }
        }
    }

    /// <summary>
    /// Waits until at least one timer is pending, which is the moment the code under test is sitting in a delay.
    /// </summary>
    /// <returns>A task that completes when a delay is pending.</returns>
    public async Task WaitForPendingTimer()
    {
        while (true)
        {
            lock (_gate)
            {
                if (_timers.Exists(_ => !_.IsDisposed))
                {
                    return;
                }
            }

            await _scheduled.Reader.ReadAsync().AsTask().WaitAsync(TimeSpan.FromSeconds(10), TimeProvider.System);
        }
    }

    /// <summary>
    /// Move the clock forward and fire every timer that came due.
    /// </summary>
    /// <param name="duration">How far.</param>
    public void Advance(TimeSpan duration)
    {
        ClockTimer[] due;
        lock (_gate)
        {
            Interlocked.Add(ref _ticks, duration.Ticks);
            _timers.RemoveAll(_ => _.IsDisposed);
            due = [.. _timers.Where(_ => _.DueTicks <= Interlocked.Read(ref _ticks))];
        }

        foreach (var timer in due)
        {
            timer.Fire();
        }
    }

    sealed class ClockTimer(TimerClock owner, TimerCallback callback, object? state, TimeSpan dueTime) : ITimer
    {
        int _disposed;

        public long DueTicks { get; private set; } = owner.GetTimestamp() + dueTime.Ticks;

        public bool IsDisposed => Volatile.Read(ref _disposed) != 0;

        public bool Change(TimeSpan dueTime, TimeSpan period)
        {
            DueTicks = owner.GetTimestamp() + dueTime.Ticks;
            return !IsDisposed;
        }

        public void Fire()
        {
            if (Interlocked.Exchange(ref _disposed, 1) == 0)
            {
                callback(state);
            }
        }

        public void Dispose() => Interlocked.Exchange(ref _disposed, 1);

        public ValueTask DisposeAsync()
        {
            Interlocked.Exchange(ref _disposed, 1);
            return ValueTask.CompletedTask;
        }
    }
}
