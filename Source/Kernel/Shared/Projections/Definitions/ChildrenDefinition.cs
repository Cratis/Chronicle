// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Models;
using Cratis.Chronicle.Properties;

namespace Cratis.Chronicle.Projections.Definitions;

/// <summary>
/// Represents the definition of a children projection.
/// </summary>
/// <param name="IdentifiedBy">Property on model that identifies the unique object, typically the key - or id (event source id).</param>
/// <param name="Model">The target <see cref="ModelDefinition"/>.</param>
/// <param name="InitialModelState">The initial values to use with the model for new instances.</param>
/// <param name="From">All the <see cref="FromDefinition"/> for <see cref="EventType">event types</see>.</param>
/// <param name="Join">All the <see cref="JoinDefinition"/> for <see cref="EventType">event types</see>.</param>
/// <param name="Children">All the <see cref="ChildrenDefinition"/> for properties on model.</param>
/// <param name="All">The full <see cref="AllDefinition"/>.</param>
/// <param name="FromEventProperty">Optional <see cref="FromEventPropertyDefinition"/> definition.</param>
/// <param name="RemovedWith">The definition of what removes a child, if any.</param>
public record ChildrenDefinition(
    PropertyPath IdentifiedBy,
    ModelDefinition Model,
    JsonObject InitialModelState,
    IDictionary<EventType, FromDefinition> From,
    IDictionary<EventType, JoinDefinition> Join,
    IDictionary<PropertyPath, ChildrenDefinition> Children,
    AllDefinition All,
    FromEventPropertyDefinition? FromEventProperty = default,
    RemovedWithDefinition? RemovedWith = default) :
    ProjectionDefinition(
        EventSequences.EventSequenceId.Unspecified,
        ProjectionId.Unspecified,
        Model,
        true,
        false,
        InitialModelState,
        From,
        Join,
        Children,
        [],
        All,
        SinkDefinition.None,
        FromEventProperty,
        RemovedWith);
