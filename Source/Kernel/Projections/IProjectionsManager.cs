// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics.CodeAnalysis;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Projections;
using Cratis.Chronicle.Concepts.Projections.Definitions;
using Cratis.Chronicle.Concepts.ReadModels;

namespace Cratis.Chronicle.Projections;

/// <summary>
/// Defines a system for managing <see cref="IProjection">projections</see>.
/// </summary>
public interface IProjectionsManager
{
    /// <summary>
    /// Register a <see cref="IProjection"/>.
    /// </summary>
    /// <param name="eventStore"><see cref="EventStoreName"/> the projection is for.</param>
    /// <param name="definitions"><see cref="IEnumerable{T}"/> of <see cref="ProjectionDefinition"/> to register.</param>
    /// <param name="readModelDefinitions"><see cref="IEnumerable{T}"/> of <see cref="ReadModelDefinition"/> for the projections.</param>
    /// <param name="namespaces"><see cref="IEnumerable{T}"/> of <see cref="EventStoreNamespaceName"/> the projection is for.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    Task Register(EventStoreName eventStore, IEnumerable<ProjectionDefinition> definitions, IEnumerable<ReadModelDefinition> readModelDefinitions, IEnumerable<EventStoreNamespaceName> namespaces);

    /// <summary>
    /// Add a namespace to the system.
    /// </summary>
    /// <param name="eventStore"><see cref="EventStoreName"/> the namespace is for.</param>
    /// <param name="namespace"><see cref="EventStoreNamespaceName"/> to add.</param>
    /// <param name="readModelDefinitions"><see cref="IEnumerable{T}"/> of <see cref="ReadModelDefinition"/> for the projections.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    Task AddNamespace(EventStoreName eventStore, EventStoreNamespaceName @namespace, IEnumerable<ReadModelDefinition> readModelDefinitions);

    /// <summary>
    /// Try to get a <see cref="IProjection"/> by <see cref="ProjectionId"/>.
    /// </summary>
    /// <param name="eventStore"><see cref="EventStoreName"/> the projection is for.</param>
    /// <param name="namespace"><see cref="EventStoreNamespaceName"/> the projection is for.</param>
    /// <param name="id"><see cref="ProjectionId"/> of the projection.</param>
    /// <param name="projection">The <see cref="IProjection"/> if it was found, null if not.</param>
    /// <returns>True if it was found, false if not.</returns>
    bool TryGet(EventStoreName eventStore, EventStoreNamespaceName @namespace, ProjectionId id, [NotNullWhen(true)] out IProjection? projection);

    /// <summary>
    /// Evict any projection for a specific projection identifier.
    /// </summary>
    /// <param name="eventStore"><see cref="EventStoreName"/> the projection is for.</param>
    /// <param name="id"><see cref="ProjectionId"/> of the projection to evict.</param>
    void Evict(EventStoreName eventStore, ProjectionId id);
}
