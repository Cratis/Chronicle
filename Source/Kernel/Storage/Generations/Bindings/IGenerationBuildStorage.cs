// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Concepts.Projections.Generations;
using Cratis.Chronicle.Concepts.Projections.Generations.Bindings;

namespace Cratis.Chronicle.Storage.Generations.Bindings;

/// <summary>
/// Owns immutable execution documents and observer claims alongside the binding catalog.
/// </summary>
public interface IGenerationBuildStorage
{
    /// <summary>
    /// Reserves the next generation, execution document, observer and destination atomically.
    /// </summary>
    /// <param name="store">The store.</param>
    /// <param name="key">The materialization.</param>
    /// <param name="execution">The immutable execution inputs.</param>
    /// <param name="operationId">The retry identity.</param>
    /// <param name="at">The original request time.</param>
    /// <returns>The reservation outcome.</returns>
    Task<GenerationReservation> ReserveNext(EventStoreName store, MaterializationKey key, MaterializationExecutionSpecification execution, GenerationBuildOperationId operationId, DateTimeOffset at);

    /// <summary>
    /// Reads the original persisted build intent without allocating or changing its lifecycle.
    /// </summary>
    /// <param name="store">The store.</param>
    /// <param name="key">The materialization.</param>
    /// <param name="generation">The reserved generation.</param>
    /// <returns>The original request, or null when no build intent was persisted.</returns>
    /// <exception cref="GenerationBuildConflict">The persisted intent is ambiguous or inconsistent with the catalog.</exception>
    /// <exception cref="NotSupportedException">The provider does not support generation build intent recovery.</exception>
    Task<GenerationBuildRequest?> GetBuildRequest(EventStoreName store, MaterializationKey key, MaterializationGeneration generation) => throw new NotSupportedException();

    /// <summary>
    /// Captures current G1 execution without asserting knowledge of its historical provenance.
    /// </summary>
    /// <param name="store">The store.</param>
    /// <param name="key">The materialization.</param>
    /// <param name="execution">Inputs captured before current registration is replaced.</param>
    /// <returns>Whether the execution was attached to an adopted G1.</returns>
    Task<bool> TryAdoptExecution(EventStoreName store, MaterializationKey key, MaterializationExecutionSpecification execution);

    /// <summary>
    /// Resolves an explicitly claimed observer. Ambiguous incarnations fail closed.
    /// </summary>
    /// <param name="store">The store.</param>
    /// <param name="ns">The namespace.</param>
    /// <param name="eventSequence">The source event sequence.</param>
    /// <param name="observerId">The opaque observer identity.</param>
    /// <returns>The binding, or null for an unbound observer.</returns>
    Task<GenerationObserverBinding?> ResolveObserver(EventStoreName store, EventStoreNamespaceName ns, EventSequenceId eventSequence, ObserverId observerId);

    /// <summary>
    /// Reads the exact execution document; never falls back to a current definition.
    /// </summary>
    /// <param name="store">The store.</param>
    /// <param name="key">The materialization.</param>
    /// <param name="generation">The generation.</param>
    /// <returns>The captured execution, or null for a generation predating snapshots.</returns>
    Task<MaterializationExecutionSpecification?> GetExecution(EventStoreName store, MaterializationKey key, MaterializationGeneration generation);
}
