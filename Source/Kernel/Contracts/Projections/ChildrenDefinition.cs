// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Events;
using Cratis.Chronicle.Contracts.Models;
using ProtoBuf;

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
    /// Gets or sets the target <see cref="ModelDefinition"/>.
    /// </summary>
    [ProtoMember(2)]
    public ModelDefinition Model { get; set; }

     /// <summary>
    /// Gets or sets the initial state to use for new instances of the model.
    /// </summary>
    [ProtoMember(3)]
    public string InitialModelState { get; set; }

    /// <summary>
    /// Gets or sets all the <see cref="FromDefinition"/> for <see cref="EventType">event types</see>.
    /// </summary>
    [ProtoMember(4)]
    public IDictionary<EventType, FromDefinition> From { get; set; }

    /// <summary>
    /// Gets or sets all the <see cref="JoinDefinition"/> for <see cref="EventType">event types</see>.
    /// </summary>
    [ProtoMember(5)]
    public IDictionary<EventType, JoinDefinition> Join { get; set; }

    /// <summary>
    /// Gets or sets all the <see cref="ChildrenDefinition"/> for properties on model.
    /// </summary>
    [ProtoMember(6)]
    public IDictionary<string, ChildrenDefinition> Children { get; set; }

    /// <summary>
    /// Gets or sets the full <see cref="AllDefinition"/>.
    /// </summary>
    [ProtoMember(7)]
    public AllDefinition All { get; set; }

    /// <summary>
    /// Gets or sets the definition of what removes a child, if any.
    /// </summary>
    [ProtoMember(8)]
    public RemovedWithDefinition? RemovedWith { get; set; }
}
