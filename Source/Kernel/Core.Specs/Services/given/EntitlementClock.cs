// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Services.given;

public class EntitlementClock : TimeProvider
{
    readonly List<Deadline> _deadlines = [];
    DateTimeOffset _now = DateTimeOffset.UnixEpoch;

    public override DateTimeOffset GetUtcNow() => _now;

    public override ITimer CreateTimer(TimerCallback callback, object? state, TimeSpan dueTime, TimeSpan period)
    {
        var timer = new Deadline(_now + dueTime, callback, state);
        _deadlines.Add(timer);
        return timer;
    }

    public void Advance(TimeSpan elapsed)
    {
        _now += elapsed;
        foreach (var timer in _deadlines.ToArray()) timer.Fire(_now);
    }

    sealed class Deadline(DateTimeOffset due, TimerCallback callback, object? state) : ITimer
    {
        bool _disposed;
        public bool Change(TimeSpan dueTime, TimeSpan period) => throw new NotSupportedException();
        public void Dispose() => _disposed = true;
        public ValueTask DisposeAsync()
        {
            _disposed = true;
            return ValueTask.CompletedTask;
        }
        internal void Fire(DateTimeOffset now)
        {
            if (!_disposed && now >= due)
            {
                _disposed = true;
                callback(state);
            }
        }
    }
}
