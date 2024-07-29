// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Events;
using ProtoBuf;

namespace Cratis.Chronicle.Contracts.Projections;

/// <summary>
/// Represents an event that has to be applied on top of the projection of an instance.
/// Typically used by aggregate roots for re-evaluating their state when applying events.
/// </summary>
[ProtoContract]
public class EventToApply
{
    /// <summary>
    /// Gets or sets the event type.
    /// </summary>
    [ProtoMember(1)]
    public EventType EventType { get; set; }

    /// <summary>
    /// Gets or sets the content of the event.
    /// </summary>
    [ProtoMember(2)]
    public string Content { get; set; }
}
