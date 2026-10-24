// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Projections.Generations;
using Cratis.Chronicle.Concepts.Projections.Generations.Bindings;
using Cratis.Chronicle.Concepts.ReadModels;
using Cratis.Chronicle.Storage.Generations.Bindings;

namespace Cratis.Chronicle.Storage.Sql.Generations;

/// <summary>
/// Represents the sql materialization binding storage.
/// </summary>
public sealed class SqlMaterializationBindingStorage : IMaterializationBindingStorage
{
    /// <inheritdoc/>
    public Task<MaterializationBinding?> Get(EventStoreName store, MaterializationKey key) => throw new MaterializationBindingsNotSupported();

    /// <inheritdoc/>
    public Task<FirstGenerationAdoption> TryAdoptFirst(EventStoreName store, MaterializationKey key, MaterializationGenerationRecord record) => throw new MaterializationBindingsNotSupported();

    /// <inheritdoc/>
    public Task<bool> TrySetBindingEpoch(EventStoreName store, MaterializationKey key, ServingEpoch expected, ServingEpoch next) => throw new MaterializationBindingsNotSupported();

    /// <inheritdoc/>
    public Task<GenerationReservation> ReserveNext(EventStoreName store, MaterializationKey key, MaterializationRevision revision, DateTimeOffset at) => throw new MaterializationBindingsNotSupported();

    /// <inheritdoc/>
    public Task<GenerationTransitionResult> TryMarkFollowing(EventStoreName store, MaterializationKey key, MaterializationGeneration generation) => throw new MaterializationBindingsNotSupported();

    /// <inheritdoc/>
    public Task<GenerationTransitionResult> TryAbandon(EventStoreName store, MaterializationKey key, MaterializationGeneration generation) => throw new MaterializationBindingsNotSupported();

    /// <inheritdoc/>
    public Task<MaterializationPublication> TryPublish(EventStoreName store, MaterializationKey key, MaterializationGeneration expectedActive, ServingEpoch expectedEpoch, MaterializationGeneration candidate, TimeSpan rollbackWindow, PublicationReceiptId receipt) => throw new MaterializationBindingsNotSupported();

    /// <inheritdoc/>
    public Task<MaterializationPublication> TryRestore(EventStoreName store, MaterializationKey key, MaterializationGeneration expectedActive, ServingEpoch expectedEpoch, MaterializationGeneration candidate, TimeSpan rollbackWindow, PublicationReceiptId receipt) => throw new MaterializationBindingsNotSupported();

    /// <inheritdoc/>
    public Task<IReadOnlyCollection<GenerationContainerClaim>> GetClaims(EventStoreName store, EventStoreNamespaceName ns, ReadModelContainerName container) => throw new MaterializationBindingsNotSupported();
}
