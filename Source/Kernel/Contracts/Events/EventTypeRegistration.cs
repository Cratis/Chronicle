// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Contracts.Events;

/// <summary>
/// Represents the payload of an event type registration.
/// </summary>
[ProtoContract]
public class EventTypeRegistration
{
    /// <summary>
    /// Gets or sets the type of the event.
    /// </summary>
    [ProtoMember(1, IsRequired = true)]
    public EventType Type { get; set; } = new EventType();

    /// <summary>
    /// Gets or sets the owner of the event type.
    /// </summary>
    public EventTypeOwner Owner { get; set; } = EventTypeOwner.Client;

    /// <summary>
    /// Gets or sets the source of the event type.
    /// </summary>
    public EventTypeSource Source { get; set; } = EventTypeSource.Code;

    /// <summary>
    /// Gets or sets the JSON schema of the event type.
    /// </summary>
    [ProtoMember(2)]
    public string Schema { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets all generations of this event type.
    /// </summary>
    [ProtoMember(3, IsRequired = true)]
    public IList<EventTypeGenerationDefinition> Generations { get; set; } = [];

    /// <summary>
    /// Gets or sets all migrations for this event type.
    /// </summary>
    [ProtoMember(4, IsRequired = true)]
    public IList<EventTypeMigrationDefinition> Migrations { get; set; } = [];

    /// <summary>
    /// Gets or sets the name of the event store this event type originates from, if it originates from a foreign event store.
    /// </summary>
    /// <remarks>
    /// When set, this indicates that the event type is produced by another event store and received via the inbox.
    /// This corresponds to the value specified on the event type's <c>EventStoreAttribute</c>.
    /// When empty, the event type belongs to the registering event store.
    /// </remarks>
    [ProtoMember(5)]
    public string EventStore { get; set; } = string.Empty;
}
