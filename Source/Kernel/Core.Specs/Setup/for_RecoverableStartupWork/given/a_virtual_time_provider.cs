// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Orleans.Hosting.for_RecoverableStartupWork.given;

/// <summary>
/// A <see cref="TimeProvider"/> whose timers fire immediately but advance a virtual clock by the requested delay,
/// so a spec can reason about elapsed backoff time without waiting it out.
/// </summary>
public sealed class a_virtual_time_provider : TimeProvider
{
    long _ticks;

    /// <inheritdoc/>
    public override long TimestampFrequency => TimeSpan.TicksPerSecond;

    /// <inheritdoc/>
    public override long GetTimestamp() => Interlocked.Read(ref _ticks);

    /// <inheritdoc/>
    public override ITimer CreateTimer(TimerCallback callback, object? state, TimeSpan dueTime, TimeSpan period)
    {
        Interlocked.Add(ref _ticks, dueTime.Ticks);
        ThreadPool.QueueUserWorkItem(_ => callback(state));
        return new virtual_timer();
    }

    sealed class virtual_timer : ITimer
    {
        public bool Change(TimeSpan dueTime, TimeSpan period) => true;

        public void Dispose()
        {
        }

        public ValueTask DisposeAsync() => ValueTask.CompletedTask;
    }
}
