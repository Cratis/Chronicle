// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Concepts.Projections.Generations;
using Cratis.Chronicle.Storage.Generations.Bindings;

namespace Cratis.Chronicle.Projections.Generations;

/// <summary>
/// Verifies generation observer bindings without interpreting observer name suffixes.
/// </summary>
/// <param name="storage">The authoritative execution and observer storage.</param>
public sealed class GenerationObserverBindings(IGenerationBuildStorage storage) : IGenerationObserverBindings
{
    /// <inheritdoc/>
    /// <exception cref="InvalidMaterializationExecution">A claimed observer has no valid execution document.</exception>
    public async Task<GenerationObserverBinding?> ResolveObserver(EventStoreName store, EventStoreNamespaceName ns, EventSequenceId eventSequence, ObserverId observerId)
    {
        var binding = await storage.ResolveObserver(store, ns, eventSequence, observerId);
        if (binding is null) return null;
        var execution = await storage.GetExecution(store, binding.Key, binding.Identity.Generation) ?? throw new InvalidMaterializationExecution();
        Verify(binding, execution, ns, eventSequence, observerId);
        return binding;
    }

    /// <summary>
    /// Verifies that a resolved binding and its execution snapshot describe the requested observer.
    /// </summary>
    /// <param name="binding">The resolved binding.</param>
    /// <param name="execution">The binding's execution snapshot.</param>
    /// <param name="ns">The requested namespace.</param>
    /// <param name="eventSequence">The requested event sequence.</param>
    /// <param name="observerId">The requested observer.</param>
    /// <exception cref="InvalidMaterializationExecution">The binding and execution do not agree.</exception>
    internal static void Verify(GenerationObserverBinding binding, MaterializationExecutionSpecification execution, EventStoreNamespaceName ns, EventSequenceId eventSequence, ObserverId observerId)
    {
        VerifyExecution(binding, execution, eventSequence);
        VerifyIdentity(binding, ns, observerId);
        execution.EnsureValid();
    }

    static void VerifyExecution(GenerationObserverBinding binding, MaterializationExecutionSpecification execution, EventSequenceId eventSequence)
    {
        if (execution.Revision != binding.Revision) throw new InvalidMaterializationExecution();
        if (execution.EventSequence != eventSequence) throw new InvalidMaterializationExecution();
        if (binding.EventSequence != eventSequence) throw new InvalidMaterializationExecution();
    }

    static void VerifyIdentity(GenerationObserverBinding binding, EventStoreNamespaceName ns, ObserverId observerId)
    {
        if (binding.Key.Namespace != ns) throw new InvalidMaterializationExecution();
        if (binding.Identity.ObserverId != observerId) throw new InvalidMaterializationExecution();
        if (binding.Key.Projection != binding.Identity.ProjectionId) throw new InvalidMaterializationExecution();
    }
}
