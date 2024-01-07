// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Kernel.Storage;
using Aksio.Cratis.Kernel.Storage.Recommendations;
using Orleans.Runtime;
using Orleans.Storage;

namespace Aksio.Cratis.Kernel.Grains.Recommendations;

/// <summary>
/// Represents an implementation of <see cref="IGrainStorage"/> for <see cref="RecommendationState"/>.
/// </summary>
public class RecommendationGrainStorageProvider : IGrainStorage
{
    readonly IStorage _storage;

    /// <summary>
    /// Initializes a new instance of the <see cref="RecommendationGrainStorageProvider"/> class.
    /// </summary>
    /// <param name="storage"><see cref="IStorage"/> for accessing underlying storage.</param>
    public RecommendationGrainStorageProvider(IStorage storage)
    {
        _storage = storage;
    }

    /// <inheritdoc/>
    public async Task ClearStateAsync<T>(string stateName, GrainId grainId, IGrainState<T> grainState)
    {
        if (grainId.TryGetGuidKey(out var recommendationId, out var keyExtension))
        {
            var key = (RecommendationKey)keyExtension!;

            var recommendationStorage = _storage.GetEventStore((string)key.MicroserviceId).GetNamespace(key.TenantId).Recommendations;
            await recommendationStorage.Remove(recommendationId);
        }
    }

    /// <inheritdoc/>
    public async Task ReadStateAsync<T>(string stateName, GrainId grainId, IGrainState<T> grainState)
    {
        if (grainId.TryGetGuidKey(out var recommendationId, out var keyExtension))
        {
            var actualGrainState = (grainState as IGrainState<RecommendationState>)!;
            var key = (RecommendationKey)keyExtension!;

            var recommendationStorage = _storage.GetEventStore((string)key.MicroserviceId).GetNamespace(key.TenantId).Recommendations;
            actualGrainState.State = await recommendationStorage.Get(recommendationId) ?? new RecommendationState();
        }
    }

    /// <inheritdoc/>
    public async Task WriteStateAsync<T>(string stateName, GrainId grainId, IGrainState<T> grainState)
    {
        if (grainId.TryGetGuidKey(out var recommendationId, out var keyExtension))
        {
            var actualGrainState = (grainState as IGrainState<RecommendationState>)!;
            var key = (RecommendationKey)keyExtension!;

            var recommendationStorage = _storage.GetEventStore((string)key.MicroserviceId).GetNamespace(key.TenantId).Recommendations;
            actualGrainState.State.Id = recommendationId;
            await recommendationStorage.Save(recommendationId, actualGrainState.State);
        }
    }
}
