// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Models;
using Cratis.Chronicle.Properties;

namespace Cratis.Chronicle.Projections.Definitions;

/// <summary>
/// Represents the definition of a projection.
/// </summary>
/// <param name="Identifier"><see cref="ProjectionId">Identifier</see> of the projection.</param>
/// <param name="Name">Friendly display name of the projection.</param>
/// <param name="Model">The target <see cref="ModelDefinition"/>.</param>
/// <param name="IsActive">Whether or not the projection is an actively observing projection.</param>
/// <param name="IsRewindable">Whether or not the projection is rewindable.</param>
/// <param name="InitialModelState">The initial state to use for new instances of the model.</param>
/// <param name="From">All the <see cref="FromDefinition"/> for <see cref="EventType">event types</see>.</param>
/// <param name="Join">All the <see cref="JoinDefinition"/> for <see cref="EventType">event types</see>.</param>
/// <param name="Children">All the <see cref="ChildrenDefinition"/> for properties on model.</param>
/// <param name="FromAny">All the <see cref="FromAnyDefinition"/> for <see cref="EventType">event types</see>.</param>
/// <param name="All">The full <see cref="AllDefinition"/>.</param>
/// <param name="FromEventProperty">Optional <see cref="FromEventPropertyDefinition"/> definition.</param>
/// <param name="RemovedWith">The definition of what removes a child, if any.</param>
/// <param name="LastUpdated">The last time the projection definition was updated.</param>
public record ProjectionDefinition(
    ProjectionId Identifier,
    ProjectionName Name,
    ModelDefinition Model,
    bool IsActive,
    bool IsRewindable,
    JsonObject InitialModelState,
    IDictionary<EventType, FromDefinition> From,
    IDictionary<EventType, JoinDefinition> Join,
    IDictionary<PropertyPath, ChildrenDefinition> Children,
    IEnumerable<FromAnyDefinition> FromAny,
    AllDefinition All,
    FromEventPropertyDefinition? FromEventProperty = default,
    RemovedWithDefinition? RemovedWith = default,
    DateTimeOffset? LastUpdated = default)
{
    /// <summary>
    /// Checks if the definition is empty or not. Empty meaning that there is no definition.
    /// </summary>
    public bool IsEmpty => From.Count == 0 && Children.Count == 0;
}
