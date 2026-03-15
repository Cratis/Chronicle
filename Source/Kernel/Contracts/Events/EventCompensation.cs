// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Identities;

namespace Cratis.Chronicle.Contracts.Events;

/// <summary>
/// Represents a compensation applied to an event.
/// </summary>
[ProtoContract]
public class EventCompensation
{
    /// <summary>
    /// Gets or sets the generation of the event type for this compensation.
    /// </summary>
    [ProtoMember(1)]
    public uint Generation { get; set; }

    /// <summary>
    /// Gets or sets the correlation id of the compensation.
    /// </summary>
    [ProtoMember(2)]
    public string CorrelationId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets who or what caused the compensation.
    /// </summary>
    [ProtoMember(3)]
    public Identity CausedBy { get; set; }

    /// <summary>
    /// Gets or sets when the compensation occurred.
    /// </summary>
    [ProtoMember(4)]
    public DateTimeOffset Occurred { get; set; }

    /// <summary>
    /// Gets or sets the JSON content of the compensating event.
    /// </summary>
    [ProtoMember(5)]
    public string Content { get; set; } = string.Empty;
}
