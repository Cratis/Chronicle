// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using ProtoBuf;

namespace Aksio.Cratis.Kernel.Contracts.Events;

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
}
