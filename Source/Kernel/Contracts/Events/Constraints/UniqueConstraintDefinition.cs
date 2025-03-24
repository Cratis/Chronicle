// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using ProtoBuf;

namespace Cratis.Chronicle.Contracts.Events.Constraints;

/// <summary>
/// Represents a unique constraint definition.
/// </summary>
[ProtoContract]
public class UniqueConstraintDefinition
{
    /// <summary>
    /// Gets or sets the event definitions for the unique constraint.
    /// </summary>
    [ProtoMember(1, IsRequired = true)]
    public IList<UniqueConstraintEventDefinition> EventDefinitions { get; set; } = [];

    /// <summary>
    /// Gets or sets whether to ignore casing.
    /// </summary>
    [ProtoMember(2)]
    public bool IgnoreCasing { get; set; }
}
