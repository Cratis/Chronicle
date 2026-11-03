// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Services.Clients.for_ConnectionService.given;

public class RegistrationClock : TimeProvider
{
    readonly TaskCompletionSource _created = new(TaskCreationOptions.RunContinuationsAsynchronously);
    readonly TaskCompletionSource _disposed = new(TaskCreationOptions.RunContinuationsAsynchronously);
    DeadlineTimer? _timer;

    public Task Created => _created.Task;
    public Task Disposed => _disposed.Task;
    public TimeSpan DueTime { get; private set; }

    public override ITimer CreateTimer(TimerCallback callback, object? state, TimeSpan dueTime, TimeSpan period)
    {
        DueTime = dueTime;
        _timer = new DeadlineTimer(callback, state, _disposed);
        _created.TrySetResult();
        return _timer;
    }

    public void Expire() => _timer?.Fire();

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
