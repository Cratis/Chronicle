// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Threading.Channels;

namespace Cratis.Chronicle.EventSequences.for_AppendedEventsQueue.when_enqueuing.with_observer_that_is_unreachable.given;

/// <summary>
/// Advances one catch-up retry timer at a time without sleeping or changing the warning limiter's clock.
/// </summary>
public class CatchupClock : TimeProvider
{
    readonly Channel<RetryTimer> _timers = Channel.CreateUnbounded<RetryTimer>();
    RetryTimer? _pending;
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
        var timer = new RetryTimer(callback, state, dueTime);
        _timers.Writer.TryWrite(timer);
        return timer;
    }

    /// <summary>
    /// Waits until a retry is scheduled, leaving that retry pending.
    /// </summary>
    /// <returns>The scheduled retry delay.</returns>
    public async Task<TimeSpan> WaitForNextDelay()
    {
        _pending ??= await _timers.Reader.ReadAsync().AsTask().WaitAsync(TimeSpan.FromSeconds(5), TimeProvider.System);
        return _pending.Delay;
    }

    /// <summary>
    /// Advances the clock by the pending retry delay and fires its one-shot timer.
    /// </summary>
    /// <returns>The elapsed retry delay.</returns>
    public async Task<TimeSpan> AdvanceNextDelay()
    {
        var delay = await WaitForNextDelay();
        var timer = _pending!;
        _pending = null;
        Interlocked.Add(ref _ticks, delay.Ticks);
        timer.Fire();
        return delay;
    }

    sealed class RetryTimer(TimerCallback callback, object? state, TimeSpan dueTime) : ITimer
    {
        int _disposed;

        /// <summary>
        /// Gets the scheduled delay.
        /// </summary>
        public TimeSpan Delay { get; private set; } = dueTime;

        /// <inheritdoc/>
        public bool Change(TimeSpan dueTime, TimeSpan period)
        {
            Delay = dueTime;
            return Volatile.Read(ref _disposed) == 0;
        }

        /// <summary>
        /// Fires this timer unless it was canceled.
        /// </summary>
        public void Fire()
        {
            if (Interlocked.Exchange(ref _disposed, 1) == 0)
            {
                callback(state);
            }
        }

        /// <inheritdoc/>
        public void Dispose() => Interlocked.Exchange(ref _disposed, 1);

        /// <inheritdoc/>
        public ValueTask DisposeAsync()
        {
            Interlocked.Exchange(ref _disposed, 1);
            return ValueTask.CompletedTask;
        }
    }
}
