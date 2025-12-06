// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Projections;

namespace Cratis.Chronicle.Storage.MongoDB.Projections;

/// <summary>
/// Represents the MongoDB document structure for a projection future.
/// </summary>
/// <param name="Id">Unique identifier for the future.</param>
/// <param name="ProjectionId">The projection this future belongs to.</param>
/// <param name="Event">The simplified event representation.</param>
/// <param name="ParentPath">The property path to the parent in the projection hierarchy (e.g., "Configurations").</param>
/// <param name="ChildPath">The property path to the child collection (e.g., "Configurations.Hubs").</param>
/// <param name="IdentifiedByProperty">The property that identifies the child (e.g., "HubId").</param>
/// <param name="ParentIdentifiedByProperty">The property that identifies the parent (e.g., "ConfigurationId").</param>
/// <param name="ParentKey">The value of the parent key from the event (e.g., ConfigurationId value).</param>
/// <param name="Created">When this future was created.</param>
public record ProjectionFuture(
    ProjectionFutureId Id,
    ProjectionId ProjectionId,
    Event Event,
    string ParentPath,
    string ChildPath,
    string IdentifiedByProperty,
    string ParentIdentifiedByProperty,
    object ParentKey,
    DateTimeOffset Created);
