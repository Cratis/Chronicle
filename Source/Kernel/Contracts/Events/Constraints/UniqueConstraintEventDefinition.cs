// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using ProtoBuf;

namespace Cratis.Chronicle.Contracts.Events.Constraints;

/// <summary>
/// Represents a unique constraint definition for a specific event.
/// </summary>
[ProtoContract]
public class UniqueConstraintEventDefinition
{
    /// <summary>
    /// Gets or sets the event type for the unique constraint.
    /// </summary>
    [ProtoMember(1)]
    public EventType EventType { get; set; }

    /// <summary>
    /// Gets or sets the property to use for the unique constraint.
    /// </summary>
    [ProtoMember(2)]
    public string Property { get; set; }
}
