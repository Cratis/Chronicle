// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Concepts.Projections.Generations;

namespace Cratis.Chronicle.Projections.Generations;

/// <summary>
/// Resolves opaque generation observers and verifies their immutable execution inputs.
/// </summary>
public interface IGenerationObserverBindings
{
    /// <summary>
    /// Resolves an explicit binding, refusing cataloged observers with missing or conflicting execution.
    /// </summary>
    /// <param name="store">The store.</param>
    /// <param name="ns">The namespace.</param>
    /// <param name="eventSequence">The source sequence.</param>
    /// <param name="observerId">The opaque observer identity.</param>
    /// <returns>The explicit binding, or null for an unbound observer.</returns>
    Task<GenerationObserverBinding?> ResolveObserver(EventStoreName store, EventStoreNamespaceName ns, EventSequenceId eventSequence, ObserverId observerId);
}
