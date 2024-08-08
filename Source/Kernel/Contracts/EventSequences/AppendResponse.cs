// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Events.Constraints;
using ProtoBuf;

namespace Cratis.Chronicle.Contracts.EventSequences;

#pragma warning disable MA0036

/// <summary>
/// Represents the response from appending an event.
/// </summary>
[ProtoContract]
public class AppendResponse
{
    /// <summary>
    /// Gets or sets the <see cref="CorrelationId"/> for the operation.
    /// </summary>
    [ProtoMember(1)]
    public string CorrelationId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the sequence number of the event that was appended, if successful.
    /// </summary>
    [ProtoMember(2)]
    public ulong SequenceNumber { get; set; }

    /// <summary>
    /// Gets a value indicating whether the operation was successful.
    /// </summary>
    [ProtoMember(3, IsRequired = true)]
    public IList<ConstraintViolation> ConstraintViolations { get; set; } = [];

    /// <summary>
    /// Gets a value indicating whether the operation was successful.
    /// </summary>
    [ProtoMember(4, IsRequired = true)]
    public IList<string> Errors { get; set; } = [];
}
