// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Events;
using Cratis.Chronicle.Contracts.Models;
using Cratis.Chronicle.Contracts.Primitives;
using ProtoBuf;

namespace Cratis.Chronicle.Contracts.Projections;

/// <summary>
/// Represents the definition of a projection.
/// </summary>
[ProtoContract]
public class ProjectionDefinition
{
    /// <summary>
    /// Gets or sets the unique identifier of the projection.
    /// </summary>
    [ProtoMember(1)]
    public Guid Identifier { get; set; }

    /// <summary>
    /// Gets or sets the friendly display name of the projection.
    /// </summary>
    [ProtoMember(2)]
    public string Name { get; set; }

    /// <summary>
    /// Gets or sets the target <see cref="ModelDefinition"/>.
    /// </summary>
    [ProtoMember(3)]
    public ModelDefinition Model { get; set; }

    /// <summary>
    /// Gets or sets whether or not the projection is an actively observing projection.
    /// </summary>
    [ProtoMember(4)]
    public bool IsActive { get; set; }

    /// <summary>
    /// Gets or sets whether or not the projection is rewindable.
    /// </summary>
    [ProtoMember(5)]
    public bool IsRewindable { get; set; }

    /// <summary>
    /// Gets or sets the initial state to use for new instances of the model.
    /// </summary>
    [ProtoMember(6)]
    public string InitialModelState { get; set; }

    /// <summary>
    /// Gets or sets all the <see cref="FromDefinition"/> for <see cref="EventType">event types</see>.
    /// </summary>
    [ProtoMember(7)]
    public IDictionary<EventType, FromDefinition> From { get; set; }

    /// <summary>
    /// Gets or sets all the <see cref="JoinDefinition"/> for <see cref="EventType">event types</see>.
    /// </summary>
    [ProtoMember(8)]
    public IDictionary<EventType, JoinDefinition> Join { get; set; }

    /// <summary>
    /// Gets or sets all the <see cref="ChildrenDefinition"/> for properties on model.
    /// </summary>
    [ProtoMember(9)]
    public IDictionary<string, ChildrenDefinition> Children { get; set; }

    /// <summary>
    /// Gets or sets all the <see cref="FromAnyDefinition"/> for <see cref="EventType">event types</see>.
    /// </summary>
    [ProtoMember(10, IsRequired = true)]
    public IList<FromAnyDefinition> FromAny { get; set; } = [];

    /// <summary>
    /// Gets or sets the full <see cref="AllDefinition"/>.
    /// </summary>
    [ProtoMember(11)]
    public AllDefinition All { get; set; }

    /// <summary>
    /// Gets or sets the optional <see cref="FromEventPropertyDefinition"/> definition.
    /// </summary>
    [ProtoMember(12)]
    public FromEventPropertyDefinition? FromEventProperty { get; set; }

    /// <summary>
    /// Gets or sets the definition of what removes a child, if any.
    /// </summary>
    [ProtoMember(13)]
    public RemovedWithDefinition? RemovedWith { get; set; }

    /// <summary>
    /// Gets or sets the last time the projection definition was updated.
    /// </summary>
    [ProtoMember(14)]
    public SerializableDateTimeOffset? LastUpdated { get; set; }
}
