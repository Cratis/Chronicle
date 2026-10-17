// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Occurrences.given;

public class ManualOccurrenceClock : TimeProvider
{
    public TaskCompletionSource<ManualTimer> Created { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

    public override ITimer CreateTimer(TimerCallback callback, object state, TimeSpan dueTime, TimeSpan period)
    {
        var timer = new ManualTimer(callback, state, dueTime);
        Created.TrySetResult(timer);
        return timer;
    }

    public sealed class ManualTimer(TimerCallback callback, object state, TimeSpan dueTime) : ITimer
    {
        public TimeSpan DueTime { get; } = dueTime;

        public bool Disposed { get; private set; }

        public void Tick() => callback(state);

        public bool Change(TimeSpan dueTime, TimeSpan period) => !Disposed;

        public void Dispose() => Disposed = true;

        public ValueTask DisposeAsync()
        {
            Disposed = true;
            return ValueTask.CompletedTask;
        }
    }
}
