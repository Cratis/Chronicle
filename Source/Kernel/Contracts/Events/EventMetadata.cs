// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using ProtoBuf;

namespace Cratis.Chronicle.Contracts.Events;

/// <summary>
/// Represents the metadata related to an event.
/// </summary>
[ProtoContract]
public class EventMetadata
{
    /// <summary>
    /// Gets or sets the sequence number of the event.
    /// </summary>
    [ProtoMember(1)]
    public ulong SequenceNumber { get; set; }

    /// <summary>
    /// Gets or sets the type of the event.
    /// </summary>
    [ProtoMember(2)]
    public EventType Type { get; set; }
}
