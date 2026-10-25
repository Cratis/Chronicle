// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Concepts.Projections;
using Cratis.Chronicle.Storage.Sinks;

namespace Cratis.Chronicle.Projections.Generations;

/// <summary>
/// Resolves later-generation observers to their immutable execution and the owner's fixed writer grant.
/// </summary>
public interface IGenerationProjectionResolver
{
    /// <summary>
    /// Resolves the generation an observer is bound to.
    /// </summary>
    /// <param name="store">The event store.</param>
    /// <param name="ns">The namespace.</param>
    /// <param name="eventSequence">The source event sequence.</param>
    /// <param name="observerId">The opaque observer identity.</param>
    /// <returns>The execution, or null when generations are disabled or the observer is unbound or bound to the first generation.</returns>
    Task<GenerationExecution?> Resolve(EventStoreName store, EventStoreNamespaceName ns, EventSequenceId eventSequence, ObserverId observerId);

    /// <summary>
    /// Admits the fixed writer grant that the durable owner issued; never acquires one.
    /// </summary>
    /// <param name="store">The event store.</param>
    /// <param name="execution">The resolved execution.</param>
    /// <returns>The sink context carrying the owner's grant.</returns>
    /// <exception cref="GenerationWriterGrantNotAdmitted">No grant is admitted for the generation.</exception>
    Task<GenerationSinkContext> Admit(EventStoreName store, GenerationExecution execution);

    /// <summary>
    /// Resolves every later generation of a projection that is following, active or retired.
    /// </summary>
    /// <param name="store">The event store.</param>
    /// <param name="ns">The namespace.</param>
    /// <param name="projection">The projection.</param>
    /// <returns>The executions to follow; empty when generations are disabled or the generation floor is not established.</returns>
    Task<IReadOnlyCollection<GenerationExecution>> ResolveCataloged(EventStoreName store, EventStoreNamespaceName ns, ProjectionId projection);
}
