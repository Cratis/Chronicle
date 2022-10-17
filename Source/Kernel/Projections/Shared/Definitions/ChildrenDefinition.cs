// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;
using Aksio.Cratis.Properties;

namespace Aksio.Cratis.Events.Projections.Definitions;

/// <summary>
/// Represents the definition of a children projection.
/// </summary>
/// <param name="IdentifiedBy">Property on model that identifies the unique object, typically the key - or id (event source id).</param>
/// <param name="Model">The target <see cref="ModelDefinition"/>.</param>
/// <param name="InitialModelState">The initial values to use with the model for new instances.</param>
/// <param name="From">All the <see cref="FromDefinition"/> for <see cref="EventType">event types</see>.</param>
/// <param name="Join">All the <see cref="JoinDefinition"/> for <see cref="EventType">event types</see>.</param>
/// <param name="Children">All the <see cref="ChildrenDefinition"/> for properties on model.</param>
/// <param name="RemovedWith">The definition of what removes a child, if any.</param>
public record ChildrenDefinition(
    PropertyPath IdentifiedBy,
    ModelDefinition Model,
    JsonObject InitialModelState,
    IDictionary<EventType, FromDefinition> From,
    IDictionary<EventType, JoinDefinition> Join,
    IDictionary<PropertyPath, ChildrenDefinition> Children,
    RemovedWithDefinition? RemovedWith) : ProjectionDefinition(Guid.Empty, string.Empty, Model, true, InitialModelState, From, Join, Children, RemovedWith);
