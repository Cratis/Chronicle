// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage;
using Cratis.Chronicle.Storage.Recommendations;
using Orleans.Storage;

namespace Cratis.Chronicle.Grains.Recommendations;

/// <summary>
/// Represents an implementation of <see cref="IGrainStorage"/> for <see cref="RecommendationState"/>.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="RecommendationGrainStorageProvider"/> class.
/// </remarks>
/// <param name="storage"><see cref="IStorage"/> for accessing underlying storage.</param>
public class RecommendationGrainStorageProvider(IStorage storage) : IGrainStorage
{
    /// <inheritdoc/>
    public async Task ClearStateAsync<T>(string stateName, GrainId grainId, IGrainState<T> grainState)
    {
        if (grainId.TryGetGuidKey(out var recommendationId, out var keyExtension))
        {
            var key = (RecommendationKey)keyExtension!;

            var recommendationStorage = storage.GetEventStore(key.EventStore).GetNamespace(key.Namespace).Recommendations;
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

            var recommendationStorage = storage.GetEventStore(key.EventStore).GetNamespace(key.Namespace).Recommendations;
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

            var recommendationStorage = storage.GetEventStore(key.EventStore).GetNamespace(key.Namespace).Recommendations;
            actualGrainState.State.Id = recommendationId;
            await recommendationStorage.Save(recommendationId, actualGrainState.State);
        }
    }
}
