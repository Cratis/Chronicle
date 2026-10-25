// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Concepts.Projections;
using Cratis.Chronicle.Concepts.Projections.Generations;

namespace Cratis.Chronicle.Storage.Generations.Bindings;

/// <summary>
/// Represents the single owner of an observer identity within an event store.
/// </summary>
/// <param name="ObserverId">The claimed observer identity.</param>
/// <param name="Claimant">Who owns the identity.</param>
/// <param name="Projection">The owning projection for a generation claim; unspecified for a registration.</param>
/// <param name="Generation">The owning generation for a generation claim; unspecified for a registration.</param>
/// <param name="Revision">The revision the generation is bound to; unknown for a registration.</param>
/// <remarks>The owner never contains the key or namespace, so one generation with one revision is one owner across namespaces.</remarks>
public sealed record ObserverIdentityClaim(ObserverId ObserverId, ObserverIdentityClaimant Claimant, ProjectionId Projection, MaterializationGeneration Generation, MaterializationRevision Revision)
{
    /// <summary>
    /// Creates the claim of a user or legacy observer definition.
    /// </summary>
    /// <param name="observerId">The observer identity.</param>
    /// <returns>The claim.</returns>
    public static ObserverIdentityClaim ForRegistration(ObserverId observerId) =>
        new(observerId, ObserverIdentityClaimant.Registration, ProjectionId.Unspecified, MaterializationGeneration.Unspecified, MaterializationRevision.Unknown);

    /// <summary>
    /// Creates the claim of a later generation.
    /// </summary>
    /// <param name="binding">The generation observer binding.</param>
    /// <returns>The claim.</returns>
    public static ObserverIdentityClaim ForGeneration(GenerationObserverBinding binding) =>
        new(binding.Identity.ObserverId, ObserverIdentityClaimant.Generation, binding.Key.Projection, binding.Identity.Generation, binding.Revision);
}
