// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Kernel.Contracts.Events;
using ProtoBuf;

namespace Aksio.Cratis.Kernel.Contracts.Observation;

/// <summary>
/// Represents the payload for registering an observer.
/// </summary>
[ProtoContract]
public class RegisterObserver
{
    /// <summary>
    /// Gets or sets the microservice identifier.
    /// </summary>
    [ProtoMember(1)]
    public string EventStoreName { get; set; }

    /// <summary>
    /// Gets or sets the tenant identifier.
    /// </summary>
    [ProtoMember(2)]
    public Guid TenantId { get; set; }

    /// <summary>
    /// Gets or sets the event sequence identifier.
    /// </summary>
    [ProtoMember(3)]
    public string EventSequenceId { get; set; }

    /// <summary>
    /// Gets or sets a collection of event types to observe.
    /// </summary>
    [ProtoMember(4)]
    public IList<EventType> EventTypes { get; set; } = new List<EventType>();
}
