// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Projections;

/// <summary>
/// A <see cref="TimeProvider"/> whose timestamp is set by the spec; one tick is one second.
/// </summary>
public class SettableTimeProvider : TimeProvider
{
    /// <summary>
    /// Gets or sets the current timestamp in seconds.
    /// </summary>
    public long Seconds { get; set; } = 1;

    /// <inheritdoc/>
    public override long TimestampFrequency => 1;

    /// <inheritdoc/>
    public override long GetTimestamp() => Seconds;
}
