// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Events;
using ProtoBuf;

namespace Cratis.Chronicle.Contracts.Projections;

/// <summary>
/// Represents the from any definition of a projection.
/// </summary>
[ProtoContract]
public class FromAnyDefinition
{
    /// <summary>
    /// Gets or sets the event types.
    /// </summary>
    [ProtoMember(1)]
    public IEnumerable<EventType> EventTypes { get; set; }

    /// <summary>
    /// Gets or sets the from definition.
    /// </summary>
    [ProtoMember(2)]
    public FromDefinition From { get; set; }
}
