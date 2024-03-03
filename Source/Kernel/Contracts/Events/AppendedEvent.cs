// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using ProtoBuf;

namespace Cratis.Kernel.Contracts.Events;

/// <summary>
/// Represents an event that has been appended to an event log.
/// </summary>
[ProtoContract]
public class AppendedEvent
{
    /// <summary>
    /// Gets the metadata for the event.
    /// </summary>
    [ProtoMember(1)]
    public EventMetadata Metadata { get; set; }

    /// <summary>
    /// Gets the context for the event.
    /// </summary>
    [ProtoMember(2)]
    public EventContext Context { get; set; }

    /// <summary>
    /// The JSON representation content of the event.
    /// </summary>
    [ProtoMember(3)]
    public string Content { get; set; }
}
