// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Contracts.Events.Constraints;

/// <summary>
/// Represents a constraint.
/// </summary>
[ProtoContract]
[ReservedProtoFields(3)]
public class Constraint
{
    /// <summary>
    /// Gets or sets the name of the constraint.
    /// </summary>
    [ProtoMember(1)]
    public string Name { get; set; }

    /// <summary>
    /// Gets or sets type of constraint.
    /// </summary>
    [ProtoMember(2)]
    public ConstraintType Type { get; set; }

    /// <summary>
    /// Gets or sets the event type identifiers that remove the constraint.
    /// </summary>
    /// <remarks>
    /// Every one of them releases the constraint on its own. Field 3 held the single event type id this replaces
    /// and is reserved so an older payload is ignored rather than misread.
    /// </remarks>
    [ProtoMember(6)]
    public IList<string> RemovedWith { get; set; } = [];

    /// <summary>
    /// Gets or sets the definition of the constraint.
    /// </summary>
    [ProtoMember(4)]
    public OneOf<UniqueConstraintDefinition, UniqueEventTypeConstraintDefinition> Definition { get; set; }

    /// <summary>
    /// Gets or sets the optional scope of the constraint.
    /// </summary>
    [ProtoMember(5)]
    public ConstraintScope? Scope { get; set; }
}
