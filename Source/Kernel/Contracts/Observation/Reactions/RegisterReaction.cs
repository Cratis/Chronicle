// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Events;
using ProtoBuf;

namespace Cratis.Chronicle.Contracts.Observation.Reactions;

/// <summary>
/// Represents the payload for registering an observer.
/// </summary>
[ProtoContract]
public class RegisterReaction
{
    /// <summary>
    /// Gets or sets the connection identifier.
    /// </summary>
    [ProtoMember(1)]
    public string ConnectionId { get; set; }

    /// <summary>
    /// Gets or sets the event store name.
    /// </summary>
    [ProtoMember(2)]
    public string EventStoreName { get; set; }

    /// <summary>
    /// Gets or sets the namespace.
    /// </summary>
    [ProtoMember(3)]
    public string Namespace { get; set; }

    /// <summary>
    /// Gets or sets the event sequence identifier.
    /// </summary>
    [ProtoMember(4)]
    public string EventSequenceId { get; set; }

    /// <summary>
    /// Gets or sets the observer identifier.
    /// </summary>
    [ProtoMember(5, IsRequired = true)]
    public string ObserverId { get; set; } = Guid.NewGuid().ToString();

    /// <summary>
    /// Gets or sets a collection of event types to observe.
    /// </summary>
    [ProtoMember(6, IsRequired = true)]
    public IEnumerable<EventType> EventTypes { get; set; } = [];
}
