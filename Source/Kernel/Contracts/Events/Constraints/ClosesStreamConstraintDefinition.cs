// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Contracts.Events.Constraints;

/// <summary>
/// Represents the closing-event declaration carried by the legacy constraints service.
/// </summary>
[ProtoContract]
public class ClosesStreamConstraintDefinition
{
    /// <summary>
    /// Gets or sets the closing event type identifiers.
    /// </summary>
    [ProtoMember(1)]
    public IList<string> EventTypeIds { get; set; } = [];

    /// <summary>
    /// Gets or sets the participating dimension mask.
    /// </summary>
    [ProtoMember(2)]
    public uint Dimensions { get; set; }

    /// <summary>
    /// Gets or sets the reopening event type identifiers.
    /// </summary>
    [ProtoMember(3)]
    public IList<string> ReopenedBy { get; set; } = [];

    /// <summary>
    /// Gets or sets the optional payload property providing the stream identifier.
    /// </summary>
    [ProtoMember(4)]
    public string? EventStreamIdFrom { get; set; }
}
