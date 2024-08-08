// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Primitives;
using ProtoBuf;

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
    /// Gets or sets the definition of the constraint.
    /// </summary>
    public OneOf<UniqueConstraintDefinition, UniqueEventTypeConstraintDefinition> Definition { get; set; }
}
