// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Properties;

namespace Aksio.Cratis.Events.Projections.Definitions;

/// <summary>
/// Represents the definition of a projection.
/// </summary>
/// <param name="Identifier"><see cref="ProjectionId">Identifier</see> of the projection.</param>
/// <param name="Name">Friendly display name of the projection.</param>
/// <param name="Model">The target <see cref="ModelDefinition"/>.</param>
/// <param name="IsRewindable">Whether or not the projection is rewindable.</param>
/// <param name="From">All the <see cref="FromDefinition"/> for <see cref="EventType">event types</see>.</param>
/// <param name="Join">All the <see cref="JoinDefinition"/> for <see cref="EventType">event types</see>.</param>
/// <param name="Children">All the <see cref="ChildrenDefinition"/> for properties on model.</param>
/// <param name="RemovedWith">The definition of what removes a child, if any.</param>
public record ProjectionDefinition(
    ProjectionId Identifier,
    ProjectionName Name,
    ModelDefinition Model,
    bool IsRewindable,
    IDictionary<EventType, FromDefinition> From,
    IDictionary<EventType, JoinDefinition> Join,
    IDictionary<PropertyPath, ChildrenDefinition> Children,
    RemovedWithDefinition? RemovedWith = default);
