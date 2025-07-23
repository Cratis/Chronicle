// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Contracts.Observation;

/// <summary>
/// Represents the state of an attempt of a failed partition. This includes representing the initial attempt that caused the failure.
/// </summary>
[ProtoContract]
public class FailedPartitionAttempt
{
    /// <summary>
    /// Gets or sets when the attempt occurred.
    /// </summary>
    [ProtoMember(1)]
    public SerializableDateTimeOffset Occurred { get; set; }

    /// <summary>
    /// Gets or sets the sequence number for the event that caused the failure.
    /// </summary>
    [ProtoMember(2)]
    public ulong SequenceNumber { get; set; }

    /// <summary>
    /// Gets or sets the error messages for the last error on this failed partition.
    /// </summary>
    [ProtoMember(3)]
    public IEnumerable<string> Messages { get; set; }

    /// <summary>
    /// Gets or sets the stack trace for the last error on this failed partition.
    /// </summary>
    [ProtoMember(4)]
    public string StackTrace { get; set; }
}
