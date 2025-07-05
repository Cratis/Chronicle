// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using ProtoBuf;

namespace Cratis.Chronicle.Contracts.EventSequences.Concurrency;

/// <summary>
/// Represents a violation of a constraint.
/// </summary>
[ProtoContract]
public class ConcurrencyViolation
{
    /// <summary>
    /// Gets or sets the identifier of the event source that caused the violation.
    /// </summary>
    [ProtoMember(1)]
    public string EventSourceId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the expected event sequence number.
    /// </summary>
    [ProtoMember(2)]
    public ulong ExpectedSequenceNumber { get; set; }

    /// <summary>
    /// Gets or sets the actual event sequence number.
    /// </summary>
    [ProtoMember(3)]
    public ulong ActualSequenceNumber { get; set; }
}
