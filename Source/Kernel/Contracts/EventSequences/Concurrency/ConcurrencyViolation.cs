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
    /// Gets or sets the expected event sequence number.
    /// </summary>
    [ProtoMember(1)]
    public ulong ExpectedSequenceNumber { get; set; }

    /// <summary>
    /// Gets or sets the actual event sequence number.
    /// </summary>
    [ProtoMember(2)]
    public ulong ActualSequenceNumber { get; set; }
}
