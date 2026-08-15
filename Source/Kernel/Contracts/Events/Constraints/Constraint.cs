// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Contracts.Events.Constraints;

/// <summary>
/// Represents a constraint.
/// </summary>
[ProtoContract]
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
    /// Every one of them releases the constraint on its own. The field held a single event type id before a
    /// constraint could be released by several, and keeps its number: a length-delimited field is never packed, so
    /// one value and a repeated field of one value are the same bytes. An older client's single removal event
    /// therefore arrives as a one-element collection, and a constraint that declares one still serializes exactly
    /// as it did.
    /// <para>
    /// Retiring the number and moving to a new one would have been a silent break in the direction the supported
    /// upgrade order takes: kernel first, then clients. The kernel would have skipped what the older client sent
    /// and registered the constraint with no removal event, so the claimed value would never be released and every
    /// later attempt to claim it would be rejected — with nothing reporting it, since the connect-time
    /// compatibility check compares services and RPC signatures and never message field shapes.
    /// </para>
    /// </remarks>
    [ProtoMember(3)]
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
