// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Services.Clients.for_ConnectionService.given;

public class RegistrationClock : TimeProvider
{
    readonly TaskCompletionSource _created = new(TaskCreationOptions.RunContinuationsAsynchronously);
    readonly TaskCompletionSource _disposed = new(TaskCreationOptions.RunContinuationsAsynchronously);
    readonly List<(TimeSpan DueTime, DeadlineTimer Timer)> _timers = [];
    DeadlineTimer? _timer;

    public Task Created => _created.Task;
    public Task Disposed => _disposed.Task;
    public TimeSpan DueTime { get; private set; }

    public override ITimer CreateTimer(TimerCallback callback, object? state, TimeSpan dueTime, TimeSpan period)
    {
        DueTime = dueTime;
        _timer = new DeadlineTimer(callback, state, _disposed);
        lock (_timers)
        {
            _timers.Add((dueTime, _timer));
        }

        _created.TrySetResult();
        return _timer;
    }

    public void Expire() => _timer?.Fire();

    /// <summary>
    /// Gets whether a timer was created that is due after the given time.
    /// </summary>
    /// <param name="dueTime">The time a timer is due after.</param>
    /// <returns>True if there is one.</returns>
    public bool HasTimerDue(TimeSpan dueTime)
    {
        lock (_timers)
        {
            return _timers.Exists(_ => _.DueTime == dueTime);
        }
    }

    /// <summary>
    /// Waits for a timer that is due after the given time to be created.
    /// </summary>
    /// <param name="dueTime">The time a timer is due after.</param>
    /// <returns>True if one was created in time.</returns>
    public async Task<bool> WaitForTimerDue(TimeSpan dueTime)
    {
        var deadline = DateTimeOffset.UtcNow + TimeSpan.FromSeconds(5);
        while (!HasTimerDue(dueTime))
        {
            if (DateTimeOffset.UtcNow > deadline)
            {
                return false;
            }

            await Task.Delay(5);
        }

        return true;
    }

    /// <summary>
    /// Fires the timers due after the given time, as if that time had passed.
    /// </summary>
    /// <param name="dueTime">The time the timers are due after.</param>
    public void ExpireDue(TimeSpan dueTime)
    {
        (TimeSpan DueTime, DeadlineTimer Timer)[] due;
        lock (_timers)
        {
            due = [.. _timers.Where(_ => _.DueTime == dueTime)];
        }

        foreach (var (_, timer) in due)
        {
            timer.Fire();
        }
    }

    public void ReleaseTimer() => _timer = null;

    sealed class DeadlineTimer(TimerCallback callback, object? state, TaskCompletionSource disposed) : ITimer
    {
        readonly Lock _lock = new();
        bool _disposed;

        public bool Change(TimeSpan dueTime, TimeSpan period) => true;

        public void Fire()
        {
            lock (_lock)
            {
                if (!_disposed)
                {
                    callback(state);
                }
            }
        }

        public void Dispose() => MarkDisposed();

        public ValueTask DisposeAsync()
        {
            MarkDisposed();
            return ValueTask.CompletedTask;
        }

        void MarkDisposed()
        {
            lock (_lock)
            {
                _disposed = true;
                disposed.TrySetResult();
            }
        }
    }
}
