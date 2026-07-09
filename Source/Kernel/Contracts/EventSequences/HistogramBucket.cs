// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Contracts.EventSequences;

/// <summary>
/// Represents a single histogram bucket for an event sequence.
/// </summary>
[ProtoContract]
public class HistogramBucket
{
    /// <summary>
    /// Gets or sets the lowest event sequence number in the bucket.
    /// </summary>
    [ProtoMember(1, IsRequired = true)]
    public ulong EventSequenceNumber { get; set; }

    /// <summary>
    /// Gets or sets the earliest occurred timestamp in the bucket.
    /// </summary>
    [ProtoMember(2, IsRequired = true)]
    public SerializableDateTimeOffset Occurred { get; set; }

    /// <summary>
    /// Gets or sets the number of events in the bucket.
    /// </summary>
    [ProtoMember(3, IsRequired = true)]
    public long Count { get; set; }
}
