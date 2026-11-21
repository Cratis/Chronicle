// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Federation.given;

/// <summary>
/// A <see cref="TimeProvider"/> whose wall clock and timestamp only move when told to.
/// </summary>
public class ControllableTimeProvider : TimeProvider
{
    DateTimeOffset _now = new(2030, 1, 1, 12, 0, 0, TimeSpan.Zero);
    long _timestamp;

    /// <inheritdoc/>
    public override long TimestampFrequency => 1000;

    /// <inheritdoc/>
    public override DateTimeOffset GetUtcNow() => _now;

    /// <inheritdoc/>
    public override long GetTimestamp() => _timestamp;

    /// <summary>
    /// Move the clock forward.
    /// </summary>
    /// <param name="duration">How far.</param>
    public void Advance(TimeSpan duration)
    {
        _now += duration;
        _timestamp += (long)(duration.TotalSeconds * TimestampFrequency);
    }
}
