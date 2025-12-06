// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Properties;

namespace Cratis.Chronicle.Concepts.Projections;

/// <summary>
/// Represents a deferred projection operation that could not be resolved immediately due to missing parent data.
/// </summary>
/// <param name="Id">Unique identifier for the future.</param>
/// <param name="ProjectionId">The projection this future belongs to.</param>
/// <param name="Event">The event that triggered this future.</param>
/// <param name="ParentPath">The property path to the parent in the projection hierarchy (e.g., "Configurations").</param>
/// <param name="ChildPath">The property path to the child collection (e.g., "Configurations.Hubs").</param>
/// <param name="IdentifiedByProperty">The property that identifies the child (e.g., "HubId").</param>
/// <param name="ParentIdentifiedByProperty">The property that identifies the parent (e.g., "ConfigurationId").</param>
/// <param name="ParentKey">The value of the parent key from the event (e.g., ConfigurationId value).</param>
/// <param name="Created">When this future was created.</param>
[GenerateSerializer]
[Alias(nameof(ProjectionFuture))]
public record ProjectionFuture(
    ProjectionFutureId Id,
    ProjectionId ProjectionId,
    AppendedEvent Event,
    PropertyPath ParentPath,
    PropertyPath ChildPath,
    PropertyPath IdentifiedByProperty,
    PropertyPath ParentIdentifiedByProperty,
    Key ParentKey,
    DateTimeOffset Created);
