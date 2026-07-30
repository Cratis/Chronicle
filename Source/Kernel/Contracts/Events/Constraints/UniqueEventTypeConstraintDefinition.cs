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
    /// Gets or sets the event type identifiers the unique constraint covers.
    /// </summary>
    /// <remarks>
    /// At most one event drawn from these types is allowed per event source. Field 1 held the single
    /// EventTypeId this replaces and is reserved so an older payload is ignored rather than misread.
    /// </remarks>
    [ProtoMember(2)]
    public IList<string> EventTypeIds { get; set; }
}
