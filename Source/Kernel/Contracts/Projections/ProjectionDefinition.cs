// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Events;

namespace Cratis.Chronicle.Contracts.Projections;

/// <summary>
/// Represents the definition of a projection.
/// </summary>
[ProtoContract]
public class ProjectionDefinition
{
    /// <summary>
    /// Gets or sets the event sequence identifier the projection projects from.
    /// </summary>
    [ProtoMember(1)]
    public string EventSequenceId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the unique identifier of the projection.
    /// </summary>
    [ProtoMember(2)]
    public string Identifier { get; set; }

    /// <summary>
    /// Gets or sets the target read model.
    /// </summary>
    [ProtoMember(3)]
    public string ReadModel { get; set; }

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
    [ProtoMember(7, IsRequired = true)]
    public IDictionary<EventType, FromDefinition> From { get; set; } = new Dictionary<EventType, FromDefinition>();

    /// <summary>
    /// Gets or sets all the <see cref="JoinDefinition"/> for <see cref="EventType">event types</see>.
    /// </summary>
    [ProtoMember(8, IsRequired = true)]
    public IDictionary<EventType, JoinDefinition> Join { get; set; } = new Dictionary<EventType, JoinDefinition>();

    /// <summary>
    /// Gets or sets all the <see cref="ChildrenDefinition"/> for properties on model.
    /// </summary>
    [ProtoMember(9, IsRequired = true)]
    public IDictionary<string, ChildrenDefinition> Children { get; set; } = new Dictionary<string, ChildrenDefinition>();

    /// <summary>
    /// Gets or sets all the <see cref="FromDerivativesDefinition"/> for <see cref="EventType">event types</see>.
    /// </summary>
    [ProtoMember(10, IsRequired = true)]
    public IList<FromDerivativesDefinition> FromEvery { get; set; } = [];

    /// <summary>
    /// Gets or sets the full <see cref="FromEveryDefinition"/>.
    /// </summary>
    [ProtoMember(11, IsRequired = true)]
    public FromEveryDefinition All { get; set; } = new();

    /// <summary>
    /// Gets or sets the optional <see cref="FromEventPropertyDefinition"/> definition.
    /// </summary>
    [ProtoMember(12)]
    public FromEventPropertyDefinition? FromEventProperty { get; set; }

    /// <summary>
    /// Gets or sets the definition of what removes a child, if any.
    /// </summary>
    [ProtoMember(13, IsRequired = true)]
    public IDictionary<EventType, RemovedWithDefinition> RemovedWith { get; set; } = new Dictionary<EventType, RemovedWithDefinition>();

    /// <summary>
    /// Gets or sets the definition of what removes a child through joining, if any.
    /// </summary>
    [ProtoMember(14, IsRequired = true)]
    public IDictionary<EventType, RemovedWithJoinDefinition> RemovedWithJoin { get; set; } = new Dictionary<EventType, RemovedWithJoinDefinition>();

    /// <summary>
    /// Gets or sets the last time the projection definition was updated.
    /// </summary>
    [ProtoMember(15)]
    public SerializableDateTimeOffset? LastUpdated { get; set; }

    /// <summary>
    /// Gets or sets the tags the projection belongs to.
    /// </summary>
    [ProtoMember(16, IsRequired = true)]
    public IList<string> Tags { get; set; } = [];
}
