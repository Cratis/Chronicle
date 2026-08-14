// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Contracts.EventSequences;

/// <summary>
/// Represents the number of events that occurred within one time bucket of an event sequence histogram.
/// </summary>
[ProtoContract]
public class HistogramBucket
{
    /// <summary>
    /// Gets or sets the inclusive start of the time bucket, truncated to the requested resolution.
    /// </summary>
    [ProtoMember(1)]
    public DateTimeOffset Occurred { get; set; }

    /// <summary>
    /// Gets or sets the number of events that occurred within the bucket.
    /// </summary>
    [ProtoMember(2)]
    public long Count { get; set; }
}
