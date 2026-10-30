// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Sql.for_LiveQuery.given;

public sealed class a_manual_time_provider : TimeProvider
{
    readonly object _gate = new();
    readonly List<(int Count, TaskCompletionSource Signal)> _waiters = [];
    TimeSpan _now;

    public List<TimeSpan> RequestedDelays { get; } = [];
    public List<manual_timer> Timers { get; } = [];

    public override ITimer CreateTimer(TimerCallback callback, object? state, TimeSpan dueTime, TimeSpan period)
    {
        manual_timer timer;
        TaskCompletionSource[] ready;
        lock (_gate)
        {
            timer = new manual_timer(callback, state, _now + dueTime);
            Timers.Add(timer);
            RequestedDelays.Add(dueTime);
            ready = _waiters.Where(waiter => waiter.Count <= Timers.Count).Select(waiter => waiter.Signal).ToArray();
            _waiters.RemoveAll(waiter => waiter.Count <= Timers.Count);
        }

        foreach (var signal in ready)
        {
            signal.TrySetResult();
        }

        return timer;
    }

    public Task WaitForTimers(int count)
    {
        lock (_gate)
        {
            if (Timers.Count >= count)
            {
                return Task.CompletedTask;
            }

            var signal = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            _waiters.Add((count, signal));
            return signal.Task.WaitAsync(TimeSpan.FromSeconds(15), TimeProvider.System);
        }
    }

    public void Advance(TimeSpan elapsed)
    {
        manual_timer[] due;
        lock (_gate)
        {
            _now += elapsed;
            due = Timers.Where(timer => !timer.IsDisposed && timer.Due <= _now).ToArray();
        }

        foreach (var timer in due)
        {
            timer.Elapse();
        }
    }

    public sealed class manual_timer(TimerCallback callback, object? state, TimeSpan due) : ITimer
    {
        int _disposed;
        int _fired;

        public TimeSpan Due { get; } = due;
        public bool IsDisposed => Volatile.Read(ref _disposed) != 0;

        public bool Change(TimeSpan dueTime, TimeSpan period) => throw new NotSupportedException();

        public void Elapse()
        {
            if (!IsDisposed && Interlocked.Exchange(ref _fired, 1) == 0)
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
