// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.SingleWriter.for_SingleWriterLease.given;

internal sealed class ManualTimer(object gate, Func<long> timestamp, TimerCallback callback, object? state) : ITimer
{
    internal long Due = long.MaxValue;
    internal TimerCallback Callback => callback;
    internal object? State => state;

    public bool Change(TimeSpan dueTime, TimeSpan period)
    {
        lock (gate)
        {
            Due = dueTime == Timeout.InfiniteTimeSpan ? long.MaxValue : timestamp() + dueTime.Ticks;
            return true;
        }
    }

    public void Dispose() => Change(Timeout.InfiniteTimeSpan, Timeout.InfiniteTimeSpan);
    public ValueTask DisposeAsync()
    {
        Change(Timeout.InfiniteTimeSpan, Timeout.InfiniteTimeSpan);
        return ValueTask.CompletedTask;
    }
}
