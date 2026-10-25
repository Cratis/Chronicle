// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Concepts.Projections.Generations;

namespace Cratis.Chronicle.Storage.Generations.Bindings;

/// <summary>
/// Defines the identity-claim aware resolution of generation observer bindings.
/// </summary>
public static class GenerationBuildStorageExtensions
{
    /// <summary>
    /// Resolves the generation observer binding of an observer, treating a rejected later-generation reservation as no binding.
    /// </summary>
    /// <param name="builds">The <see cref="IGenerationBuildStorage"/>.</param>
    /// <param name="claims">The <see cref="IObserverIdentityClaims"/>; when absent the stored binding is returned as it is.</param>
    /// <param name="store">The event store.</param>
    /// <param name="ns">The event store namespace.</param>
    /// <param name="eventSequence">The event sequence.</param>
    /// <param name="observerId">The observer identity.</param>
    /// <returns>
    /// The binding when it is a first-generation binding or a later-generation binding that owns the observer identity
    /// (no claim yet, or its own claim); otherwise null.
    /// </returns>
    public static async Task<GenerationObserverBinding?> ResolveOwnedObserver(
        this IGenerationBuildStorage builds,
        IObserverIdentityClaims? claims,
        EventStoreName store,
        EventStoreNamespaceName ns,
        EventSequenceId eventSequence,
        ObserverId observerId)
    {
        var binding = await builds.ResolveObserver(store, ns, eventSequence, observerId);
        if (binding?.Identity.Generation.IsFirst != false || claims is null) return binding;

        var stored = await claims.Get(store, observerId);
        return stored is null || stored == ObserverIdentityClaim.ForGeneration(binding) ? binding : null;
    }
}
