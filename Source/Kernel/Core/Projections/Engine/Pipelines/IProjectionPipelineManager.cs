// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Projections;
using Cratis.Chronicle.Concepts.Projections.Generations;
using Cratis.Chronicle.Storage.Sinks;
using EngineProjection = Cratis.Chronicle.Projections.Engine.IProjection;

namespace Cratis.Chronicle.Projections.Engine.Pipelines;

/// <summary>
/// Defines a system for working with <see cref="IProjectionPipeline">projection pipelines</see>.
/// </summary>
public interface IProjectionPipelineManager
{
    /// <summary>
    /// Create a projection pipeline for a given <see cref="EngineProjection"/>.
    /// </summary>
    /// <param name="eventStore">The <see cref="EventStoreName"/> the pipeline is for.</param>
    /// <param name="namespace">The <see cref="EventStoreNamespaceName"/> the pipeline is for.</param>
    /// <param name="projection"><see cref="EngineProjection"/> the pipeline is for.</param>
    /// <returns>The <see cref="IProjectionPipeline"/> instance.</returns>
    Task<IProjectionPipeline> GetFor(EventStoreName eventStore, EventStoreNamespaceName @namespace, EngineProjection projection);

    /// <summary>
    /// Create a projection pipeline that builds a specific materialization generation into its own destination.
    /// </summary>
    /// <param name="eventStore">The <see cref="EventStoreName"/> the pipeline is for.</param>
    /// <param name="namespace">The <see cref="EventStoreNamespaceName"/> the pipeline is for.</param>
    /// <param name="projection"><see cref="EngineProjection"/> the pipeline is for.</param>
    /// <param name="generation">The <see cref="MaterializationGenerationIdentity"/> the pipeline builds.</param>
    /// <returns>The <see cref="IProjectionPipeline"/> instance.</returns>
    /// <remarks>
    /// The pipeline, its handle lock, its futures and its changesets are keyed by the generation, and its sink addresses
    /// the generation's destination explicitly. It never uses the sink's replay mode.
    /// </remarks>
    Task<IProjectionPipeline> GetFor(EventStoreName eventStore, EventStoreNamespaceName @namespace, EngineProjection projection, MaterializationGenerationIdentity generation);

    /// <summary>
    /// Gets a pipeline bound to immutable execution and an explicit fixed writer grant.
    /// </summary>
    /// <param name="eventStore">The event store.</param>
    /// <param name="namespace">The namespace.</param>
    /// <param name="projection">The projection reconstructed for this execution.</param>
    /// <param name="context">The immutable execution and owner-issued admission.</param>
    /// <returns>The generation pipeline, isolated from legacy and other grant caches.</returns>
    /// <exception cref="Cratis.Chronicle.Storage.Generations.Bindings.MaterializationBindingsNotSupported">This manager cannot create generation writers.</exception>
    Task<IProjectionPipeline> GetFor(EventStoreName eventStore, EventStoreNamespaceName @namespace, EngineProjection projection, GenerationSinkContext context) =>
        throw new Cratis.Chronicle.Storage.Generations.Bindings.MaterializationBindingsNotSupported();

    /// <summary>
    /// Evict any projection pipeline for a specific projection identifier.
    /// </summary>
    /// <param name="eventStore"><see cref="EventStoreName"/> the projection is for.</param>
    /// <param name="namespace">The <see cref="EventStoreNamespaceName"/> the projection is for.</param>
    /// <param name="id"><see cref="ProjectionId"/> of the projection to evict.</param>
    void EvictFor(EventStoreName eventStore, EventStoreNamespaceName @namespace, ProjectionId id);

    /// <summary>
    /// Evict any projection pipeline for a specific materialization generation.
    /// </summary>
    /// <param name="eventStore"><see cref="EventStoreName"/> the projection is for.</param>
    /// <param name="namespace">The <see cref="EventStoreNamespaceName"/> the projection is for.</param>
    /// <param name="generation">The <see cref="MaterializationGenerationIdentity"/> to evict.</param>
    void EvictFor(EventStoreName eventStore, EventStoreNamespaceName @namespace, MaterializationGenerationIdentity generation);

    /// <summary>
    /// Clears all cached projection pipelines.
    /// </summary>
    void Clear();
}
