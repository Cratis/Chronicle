// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Contracts.Events.Constraints;

/// <summary>
/// Represents a unique constraint definition.
/// </summary>
[ProtoContract]
public class UniqueEventTypeConstraintDefinition
{
    /// <summary>
    /// Gets or sets the event type identifier for the unique constraint.
    /// </summary>
    [ProtoMember(1)]
    public string EventTypeId { get; set; }
}
