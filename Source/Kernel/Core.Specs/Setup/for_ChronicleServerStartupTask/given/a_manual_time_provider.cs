// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Orleans.Hosting.for_ChronicleServerStartupTask.given;

/// <summary>
/// A clock whose single deadline fires only after the spec observes the operation it bounds.
/// </summary>
public sealed class a_manual_time_provider : TimeProvider
{
    Action _expire = () => { };
    long _elapsed;

    /// <inheritdoc/>
    public override long TimestampFrequency => TimeSpan.TicksPerSecond;

    /// <inheritdoc/>
    public override long GetTimestamp() => _elapsed;

    /// <inheritdoc/>
    public override ITimer CreateTimer(TimerCallback callback, object? state, TimeSpan dueTime, TimeSpan period)
    {
        _expire = () =>
        {
            _elapsed += dueTime.Ticks;
            callback(state);
        };
        return Substitute.For<ITimer>();
    }

    /// <summary>
    /// Advances to the deadline and invokes its callback synchronously.
    /// </summary>
    public void Expire() => _expire();
}
