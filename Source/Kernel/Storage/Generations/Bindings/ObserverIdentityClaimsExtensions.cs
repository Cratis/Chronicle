// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Projections.Generations;
using Cratis.Chronicle.Concepts.Projections.Generations.Bindings;

namespace Cratis.Chronicle.Storage.Generations.Bindings;

/// <summary>
/// Defines the post-commit observer identity claim of a generation reservation.
/// </summary>
public static class ObserverIdentityClaimsExtensions
{
    const int MaxProbes = 256;

    /// <summary>
    /// Claims the observer identity of a created later-generation reservation, after its reservation committed.
    /// </summary>
    /// <param name="claims">The <see cref="IObserverIdentityClaims"/>.</param>
    /// <param name="bindings">The <see cref="IMaterializationBindingStorage"/> holding the reservation.</param>
    /// <param name="store">The event store.</param>
    /// <param name="key">The materialization key.</param>
    /// <param name="execution">The execution the reservation was made for.</param>
    /// <param name="reservation">The committed reservation.</param>
    /// <returns>The reservation, unchanged.</returns>
    /// <exception cref="GenerationBuildConflict">Another owner holds the identity; the reserved generation is abandoned.</exception>
    public static async Task<GenerationReservation> ClaimFor(
        this IObserverIdentityClaims claims,
        IMaterializationBindingStorage bindings,
        EventStoreName store,
        MaterializationKey key,
        MaterializationExecutionSpecification execution,
        GenerationReservation reservation)
    {
        var record = reservation.Record;
        if (reservation.Result != GenerationReservationResult.ReservationCreated || record?.Identity.Generation.IsFirst != false) return reservation;

        var current = (await bindings.Get(store, key))?.Generations.FirstOrDefault(_ => _.Identity.Generation == record.Identity.Generation);
        if (current?.State == MaterializationGenerationState.Abandoned) return reservation;

        var ours = ObserverIdentityClaim.ForGeneration(new GenerationObserverBinding(key, record.Identity, execution.EventSequence, execution.Revision));
        var stored = await claims.Get(store, record.Identity.ObserverId);
        if (stored == ours) return reservation;

        var conflict = stored is not null || await claims.HasDefinition(store, record.Identity.ObserverId);
        if (!conflict)
        {
            conflict = await claims.Claim(store, ours) != ours;
            if (!conflict && await claims.HasDefinition(store, record.Identity.ObserverId))
            {
                await claims.Release(store, ours);
                conflict = true;
            }
        }

        if (!conflict) return reservation;
        await bindings.TryAbandon(store, key, record.Identity.Generation);
        throw new GenerationBuildConflict();
    }

    /// <summary>
    /// Allocates the generation number a binding reserves next, durably bound to a revision for the whole event store.
    /// </summary>
    /// <param name="claims">The <see cref="IObserverIdentityClaims"/>.</param>
    /// <param name="store">The event store.</param>
    /// <param name="binding">The binding the number is allocated for.</param>
    /// <param name="revision">The revision the number is bound to.</param>
    /// <returns>The allocated generation, above the binding's next number.</returns>
    /// <remarks>
    /// Probes upward from the number after the binding's next one. A number already claimed for the same revision is shared,
    /// because the identity is store-wide. A number claimed for another revision, by a registration, or holding a legacy definition is skipped.
    /// The observer identity is formatted from the number, never parsed from an identifier.
    /// </remarks>
    /// <exception cref="GenerationBuildConflict">No free number was found within the probe limit.</exception>
    public static async Task<MaterializationGeneration> AllocateGeneration(
        this IObserverIdentityClaims claims,
        EventStoreName store,
        MaterializationBinding binding,
        MaterializationRevision revision)
    {
        var generation = binding.Next;
        for (var probe = 0; probe < MaxProbes; probe++)
        {
            generation = generation.Next();
            var observerId = MaterializationGenerationIdentity.For(binding.Key.Projection, generation, binding.Container).ObserverId;
            var ours = new ObserverIdentityClaim(observerId, ObserverIdentityClaimant.Generation, binding.Key.Projection, generation, revision);
            var stored = await claims.Get(store, observerId);
            if (stored == ours) return generation;
            if (stored is not null || await claims.HasDefinition(store, observerId)) continue;
            if (await claims.Claim(store, ours) != ours) continue;
            if (!await claims.HasDefinition(store, observerId)) return generation;
            await claims.Release(store, ours);
        }

        throw new GenerationBuildConflict();
    }
}
