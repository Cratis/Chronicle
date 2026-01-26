// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Events;

namespace Cratis.Chronicle.Contracts.Projections;

/// <summary>
/// Represents the definition of a children projection.
/// </summary>
[ProtoContract]
public class ChildrenDefinition
{
    /// <summary>
    /// Gets or sets the property on model that identifies the unique object, typically the key - or id (event source id).
    /// </summary>
    [ProtoMember(1)]
    public string IdentifiedBy { get; set; }

    /// <summary>
    /// Gets or sets all the <see cref="FromDefinition"/> for <see cref="EventType">event types</see>.
    /// </summary>
    [ProtoMember(2, IsRequired = true)]
    public IDictionary<EventType, FromDefinition> From { get; set; } = new Dictionary<EventType, FromDefinition>();

    /// <summary>
    /// Gets or sets all the <see cref="JoinDefinition"/> for <see cref="EventType">event types</see>.
    /// </summary>
    [ProtoMember(3, IsRequired = true)]
    public IDictionary<EventType, JoinDefinition> Join { get; set; } = new Dictionary<EventType, JoinDefinition>();

    /// <summary>
    /// Gets or sets all the <see cref="ChildrenDefinition"/> for properties on model.
    /// </summary>
    [ProtoMember(4, IsRequired = true)]
    public IDictionary<string, ChildrenDefinition> Children { get; set; } = new Dictionary<string, ChildrenDefinition>();

    /// <summary>
    /// Gets or sets the full <see cref="FromEveryDefinition"/>.
    /// </summary>
    [ProtoMember(5, IsRequired = true)]
    public FromEveryDefinition All { get; set; } = new();

    /// <summary>
    /// Gets or sets the optional <see cref="FromEventPropertyDefinition"/> definition.
    /// </summary>
    [ProtoMember(6)]
    public FromEventPropertyDefinition? FromEventProperty { get; set; }

    /// <summary>
    /// Gets or sets the definition of what removes a child, if any.
    /// </summary>
    [ProtoMember(7, IsRequired = true)]
    public IDictionary<EventType, RemovedWithDefinition> RemovedWith { get; set; } = new Dictionary<EventType, RemovedWithDefinition>();

    /// <summary>
    /// Gets or sets the definition of what removes a child, if any.
    /// </summary>
    [ProtoMember(8, IsRequired = true)]
    public IDictionary<EventType, RemovedWithJoinDefinition> RemovedWithJoin { get; set; } = new Dictionary<EventType, RemovedWithJoinDefinition>();

    /// <summary>
    /// Gets or sets whether properties should be auto-mapped from events.
    /// </summary>
    [ProtoMember(9)]
    public AutoMap AutoMap { get; set; }
}
