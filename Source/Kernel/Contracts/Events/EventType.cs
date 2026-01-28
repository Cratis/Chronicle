// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Contracts.Events;

/// <summary>
/// Represents the payload of an event type.
/// </summary>
[ProtoContract]
public class EventType
{
    /// <summary>
    /// Gets or sets the unique identifier of the event type.
    /// </summary>
    [ProtoMember(1)]
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the generation of the event type.
    /// </summary>
    [ProtoMember(2)]
    public uint Generation { get; set; }

    /// <summary>
    /// Gets or sets whether the event is a tombstone event.
    /// </summary>
    [ProtoMember(3)]
    public bool Tombstone { get; set; }
}
