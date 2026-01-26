// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.EventTypes;
using Cratis.Chronicle.Concepts.Projections.Definitions;
using Cratis.Chronicle.Concepts.ReadModels;

namespace Cratis.Chronicle.Projections;

/// <summary>
/// Defines a factory for creating <see cref="IProjection"/>.
/// </summary>
public interface IProjectionFactory
{
    /// <summary>
    /// Create a <see cref="IProjection"/> from a <see cref="ProjectionDefinition"/>.
    /// </summary>
    /// <param name="eventStore"><see cref="EventStoreName"/> to create from.</param>
    /// <param name="namespace"><see cref="EventStoreNamespaceName"/> to create from.</param>
    /// <param name="definition"><see cref="ProjectionDefinition"/> to create from.</param>
    /// <param name="readModelDefinition"><see cref="ReadModelDefinition"/> for the projection.</param>
    /// <param name="eventTypeSchemas">Available <see cref="EventTypeSchema">event type schemas</see>.</param>
    /// <returns>A new <see cref="IProjection"/> instance.</returns>
    Task<IProjection> Create(
        EventStoreName eventStore,
        EventStoreNamespaceName @namespace,
        ProjectionDefinition definition,
        ReadModelDefinition readModelDefinition,
        IEnumerable<EventTypeSchema> eventTypeSchemas);
}
