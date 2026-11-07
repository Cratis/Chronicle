// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Projections;

/// <summary>
/// Kernel infrastructure: tells the projection pipeline of one projection whether anyone is watching it, so work that
/// only serves watchers is skipped when nobody does. Checking the gate does not allocate, lock or call a grain.
/// </summary>
/// <param name="timeProvider">The <see cref="TimeProvider"/> the open period is measured with.</param>
/// <remarks>
/// The gate is opened for a limited time and has to be renewed; a watcher that disappears without being noticed
/// therefore only costs a little extra work until the gate lapses.
/// </remarks>
public sealed class ProjectionWatchGate(TimeProvider timeProvider)
{
    readonly TimeProvider _timeProvider = timeProvider;
    long _openUntil;

    /// <summary>
    /// Gets a gate that is never open.
    /// </summary>
    public static ProjectionWatchGate Closed { get; } = new(TimeProvider.System);

    /// <summary>
    /// Gets a value indicating whether the projection is currently watched.
    /// </summary>
    public bool IsOpen => Volatile.Read(ref _openUntil) is var until && until != 0 && until > _timeProvider.GetTimestamp();

    /// <summary>
    /// Opens the gate for the given period, replacing any earlier one.
    /// </summary>
    /// <param name="timeToLive">How long the gate stays open unless it is renewed.</param>
    public void Open(TimeSpan timeToLive)
    {
        if (this == Closed)
        {
            return;
        }

        var ticks = (long)(timeToLive.TotalSeconds * _timeProvider.TimestampFrequency);
        Volatile.Write(ref _openUntil, _timeProvider.GetTimestamp() + ticks);
    }

    /// <summary>
    /// Closes the gate.
    /// </summary>
    public void Close() => Volatile.Write(ref _openUntil, 0);
}
