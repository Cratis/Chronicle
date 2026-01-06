// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.ReadModels;
using Cratis.Chronicle.Properties;

namespace Cratis.Chronicle.Concepts.Projections.Definitions;

/// <summary>
/// Represents the definition of a children projection.
/// </summary>
/// <param name="IdentifiedBy">Property on model that identifies the unique object, typically the key - or id (event source id).</param>
/// <param name="From">All the <see cref="FromDefinition"/> for <see cref="EventType">event types</see>.</param>
/// <param name="Join">All the <see cref="JoinDefinition"/> for <see cref="EventType">event types</see>.</param>
/// <param name="Children">All the <see cref="ChildrenDefinition"/> for properties on model.</param>
/// <param name="All">The full <see cref="FromEveryDefinition"/>.</param>
/// <param name="RemovedWith">All the <see cref="RemovedWithDefinition"/> for <see cref="EventType">event types</see>.</param>
/// <param name="RemovedWithJoin">All the <see cref="RemovedWithJoinDefinition"/> for <see cref="EventType">event types</see>.</param>
/// <param name="FromEventProperty">Optional <see cref="FromEventPropertyDefinition"/> definition.</param>
public record ChildrenDefinition(
    PropertyPath IdentifiedBy,
    IDictionary<EventType, FromDefinition> From,
    IDictionary<EventType, JoinDefinition> Join,
    IDictionary<PropertyPath, ChildrenDefinition> Children,
    FromEveryDefinition All,
    IDictionary<EventType, RemovedWithDefinition> RemovedWith,
    IDictionary<EventType, RemovedWithJoinDefinition> RemovedWithJoin,
    FromEventPropertyDefinition? FromEventProperty = default) :
    ProjectionDefinition(
        ProjectionOwner.Parent,
        EventSequences.EventSequenceId.Unspecified,
        ProjectionId.Unspecified,
        ReadModelIdentifier.NotSet,
        true,
        false,
        new JsonObject(),
        From,
        Join,
        Children,
        [],
        All,
        RemovedWith,
        RemovedWithJoin,
        FromEventProperty)
{
    /// <summary>
    /// Gets or sets whether properties should be auto-mapped from events.
    /// </summary>
    public AutoMap AutoMap { get; set; } = AutoMap.Inherit;
}

