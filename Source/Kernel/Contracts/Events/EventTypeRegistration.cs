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
}
