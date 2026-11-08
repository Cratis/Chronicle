// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Federation.given;

/// <summary>
/// A <see cref="TimeProvider"/> whose clock only moves when told to.
/// </summary>
public class ManualTimeProvider : TimeProvider
{
    long _timestamp;

    /// <inheritdoc/>
    public override long TimestampFrequency => 1000;

    /// <inheritdoc/>
    public override long GetTimestamp() => _timestamp;

    /// <summary>
    /// Move the clock forward.
    /// </summary>
    /// <param name="duration">How far.</param>
    public void Advance(TimeSpan duration) => _timestamp += (long)(duration.TotalSeconds * TimestampFrequency);
}
