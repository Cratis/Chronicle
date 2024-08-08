// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using ProtoBuf;

namespace Cratis.Chronicle.Contracts.Events.Constraints;

/// <summary>
/// Represents a violation of a constraint.
/// </summary>
[ProtoContract]
public class ConstraintViolation
{
    /// <summary>
    /// Gets the <see cref="EventType"/> that caused the violation.
    /// </summary>
    [ProtoMember(1)]
    public EventType EventType { get; init; }

    /// <summary>
    /// Gets or sets the sequence number where the violation occurred.
    /// </summary>
    [ProtoMember(2)]
    public ulong SequenceNumber { get; set; }

    /// <summary>
    /// Gets or sets the type of constraint that was violated.
    /// </summary>
    [ProtoMember(3)]
    public ConstraintType ConstraintType { get; set; }

    /// <summary>
    /// Gets or sets the name of the constraint that was violated.
    /// </summary>
    [ProtoMember(4)]
    public string ConstraintName { get; set; }

    /// <summary>
    /// Gets or sets the message with more details.
    /// </summary>
    [ProtoMember(5)]
    public string Message { get; set; }

    /// <summary>
    /// Gets or sets the details with more details.
    /// </summary>
    [ProtoMember(6, IsRequired = true)]
    public IDictionary<string, string> Details { get; set; } = new Dictionary<string, string>();
}
