// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Namespaces.for_NamespaceCommitRetries.given;

/// <summary>A per-spec clock whose one-shot cancellation timers fire only when explicitly advanced.</summary>
public class RetryClock : TimeProvider
{
    readonly List<(DateTimeOffset Due, Action Fire)> _timers = [];
    DateTimeOffset _now = DateTimeOffset.UnixEpoch;

    /// <inheritdoc/>
    public override DateTimeOffset GetUtcNow() => _now;

    /// <inheritdoc/>
    public override ITimer CreateTimer(TimerCallback callback, object? state, TimeSpan dueTime, TimeSpan period)
    {
        var timer = Substitute.For<ITimer>();
        var disposed = false;
        timer.When(value => value.Dispose()).Do(_ => disposed = true);
        _timers.Add((_now + dueTime, () =>
        {
            if (!disposed) callback(state);
        }
        ));
        return timer;
    }

    /// <summary>Advances time and synchronously fires due one-shot timers.</summary>
    /// <param name="duration">The elapsed time.</param>
    public void Advance(TimeSpan duration)
    {
        _now += duration;
        foreach (var timer in _timers.Where(timer => timer.Due <= _now).ToArray())
        {
            _timers.Remove(timer);
            timer.Fire();
        }
    }
}
