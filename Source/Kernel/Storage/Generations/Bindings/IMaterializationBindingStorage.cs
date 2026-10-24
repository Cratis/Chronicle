// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Projections.Generations;
using Cratis.Chronicle.Concepts.Projections.Generations.Bindings;
using Cratis.Chronicle.Concepts.ReadModels;

namespace Cratis.Chronicle.Storage.Generations.Bindings;

/// <summary>
/// Represents the i materialization binding storage.
/// </summary>
public interface IMaterializationBindingStorage
{
    /// <summary>
    /// Reads the authoritative materialization binding.
    /// </summary>
    /// <param name="store">The store.</param>
    /// <param name="key">The key.</param>
    /// <returns>The recorded storage outcome.</returns>
    Task<MaterializationBinding?> Get(EventStoreName store, MaterializationKey key);

    /// <summary>
    /// Adopts an existing first generation without replacing a conflicting adoption.
    /// </summary>
    /// <param name="store">The store.</param>
    /// <param name="key">The key.</param>
    /// <param name="record">The record.</param>
    /// <returns>The recorded storage outcome.</returns>
    Task<FirstGenerationAdoption> TryAdoptFirst(EventStoreName store, MaterializationKey key, MaterializationGenerationRecord record);

    /// <summary>
    /// Conditionally establishes the serving epoch; repeating the target succeeds.
    /// </summary>
    /// <param name="store">The store.</param>
    /// <param name="key">The key.</param>
    /// <param name="expected">The expected.</param>
    /// <param name="next">The next.</param>
    /// <returns>The recorded storage outcome.</returns>
    Task<bool> TrySetBindingEpoch(EventStoreName store, MaterializationKey key, ServingEpoch expected, ServingEpoch next);

    /// <summary>
    /// Reserves a monotonically numbered candidate, or returns the in-flight candidate.
    /// </summary>
    /// <param name="store">The store.</param>
    /// <param name="key">The key.</param>
    /// <param name="revision">The revision.</param>
    /// <param name="at">The at.</param>
    /// <returns>The recorded storage outcome.</returns>
    Task<GenerationReservation> ReserveNext(EventStoreName store, MaterializationKey key, MaterializationRevision revision, DateTimeOffset at);

    /// <summary>
    /// Moves a Building candidate to Following.
    /// </summary>
    /// <param name="store">The store.</param>
    /// <param name="key">The key.</param>
    /// <param name="generation">The generation.</param>
    /// <returns>The recorded storage outcome.</returns>
    Task<GenerationTransitionResult> TryMarkFollowing(EventStoreName store, MaterializationKey key, MaterializationGeneration generation);

    /// <summary>
    /// Abandons an unserved candidate.
    /// </summary>
    /// <param name="store">The store.</param>
    /// <param name="key">The key.</param>
    /// <param name="generation">The generation.</param>
    /// <returns>The recorded storage outcome.</returns>
    Task<GenerationTransitionResult> TryAbandon(EventStoreName store, MaterializationKey key, MaterializationGeneration generation);

    /// <summary>
    /// Publishes a Following candidate atomically with gate revocation and a durable receipt.
    /// </summary>
    /// <param name="store">The store.</param>
    /// <param name="key">The key.</param>
    /// <param name="expectedActive">The expectedActive.</param>
    /// <param name="expectedEpoch">The expectedEpoch.</param>
    /// <param name="candidate">The candidate.</param>
    /// <param name="receipt">The receipt.</param>
    /// <returns>The recorded storage outcome.</returns>
    Task<MaterializationPublication> TryPublish(EventStoreName store, MaterializationKey key, MaterializationGeneration expectedActive, ServingEpoch expectedEpoch, MaterializationGeneration candidate, PublicationReceiptId receipt);

    /// <summary>
    /// Finds every generation claiming the physical container.
    /// </summary>
    /// <param name="store">The store.</param>
    /// <param name="ns">The ns.</param>
    /// <param name="container">The container.</param>
    /// <returns>The recorded storage outcome.</returns>
    Task<IReadOnlyCollection<GenerationContainerClaim>> GetClaims(EventStoreName store, EventStoreNamespaceName ns, ReadModelContainerName container);
}
