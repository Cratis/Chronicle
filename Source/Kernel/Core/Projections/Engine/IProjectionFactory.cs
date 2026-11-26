// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.EventTypes;
using Cratis.Chronicle.Concepts.Projections;
using Cratis.Chronicle.Concepts.Projections.Definitions;
using Cratis.Chronicle.Concepts.ReadModels;

namespace Cratis.Chronicle.Projections.Engine;

/// <summary>
/// Defines a factory for creating <see cref="IProjection"/>.
/// </summary>
public interface IProjectionFactory
{
    /// <summary>
    /// Creates the run of a projection for a namespace, sharing the compiled plan with every other namespace.
    /// </summary>
    /// <param name="eventStore">The <see cref="EventStoreName"/> the projection belongs to.</param>
    /// <param name="namespace">The <see cref="EventStoreNamespaceName"/> the run reads joined events from and projects for.</param>
    /// <param name="definition">The <see cref="ProjectionDefinition"/>.</param>
    /// <param name="readModelDefinition">The <see cref="ReadModelDefinition"/> the projection targets.</param>
    /// <returns>The <see cref="IProjection"/> for the namespace.</returns>
    /// <remarks>
    /// The schemas of the event store are read only when no valid plan is held for the definition, the read model and the current event types.
    /// </remarks>
    Task<IProjection> Create(
        EventStoreName eventStore,
        EventStoreNamespaceName @namespace,
        ProjectionDefinition definition,
        ReadModelDefinition readModelDefinition);

    /// <summary>
    /// Creates the run of a projection for a namespace from a plan that was compiled already.
    /// </summary>
    /// <param name="eventStore">The <see cref="EventStoreName"/> the projection belongs to.</param>
    /// <param name="namespace">The <see cref="EventStoreNamespaceName"/> the run reads joined events from and projects for.</param>
    /// <param name="plan">The <see cref="ProjectionPlan"/> to run.</param>
    /// <returns>The <see cref="IProjection"/> for the namespace.</returns>
    Task<IProjection> Create(
        EventStoreName eventStore,
        EventStoreNamespaceName @namespace,
        ProjectionPlan plan);

    /// <summary>
    /// Forgets the plans held for a projection.
    /// </summary>
    /// <param name="eventStore">The <see cref="EventStoreName"/> the projection belongs to.</param>
    /// <param name="id">The <see cref="ProjectionId"/> of the projection.</param>
    void Evict(EventStoreName eventStore, ProjectionId id);

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
