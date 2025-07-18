// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Recommendations;
using Cratis.Chronicle.Storage;

namespace Cratis.Chronicle.Grains.Recommendations;

/// <summary>
/// Represents an implementation of <see cref="IRecommendationsManager"/> that has a result.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="RecommendationsManager"/> class.
/// </remarks>
/// <param name="storage"><see cref="IStorage"/> for accessing underlying storage.</param>
public class RecommendationsManager(IStorage storage) : Grain, IRecommendationsManager
{
    /// <inheritdoc/>
    public async Task<RecommendationId> Add<TRecommendation, TRequest>(RecommendationDescription description, TRequest request)
        where TRecommendation : IRecommendation<TRequest>
        where TRequest : class, IRecommendationRequest
    {
        var id = RecommendationId.New();
        var recommendation = GrainFactory.GetGrain<TRecommendation>(id, keyExtension: GetRecommendationKey());
        await recommendation.Initialize(description, request);
        return id;
    }

    /// <inheritdoc/>
    public async Task Ignore(RecommendationId recommendationId)
    {
        var recommendation = await GetGrainFor(recommendationId);
        await recommendation.Ignore();
    }

    /// <inheritdoc/>
    public async Task Perform(RecommendationId recommendationId)
    {
        var recommendation = await GetGrainFor(recommendationId);
        await recommendation.Perform();
    }

    async Task<IRecommendation> GetGrainFor(RecommendationId recommendationId)
    {
        var key = GetRecommendationKey();
        var recommendationStorage = storage.GetEventStore(key.EventStore).GetNamespace(key.Namespace).Recommendations;
        var recommendationState = await recommendationStorage.Get(recommendationId) ?? throw new UnknownRecommendation(key.EventStore, key.Namespace, recommendationId);
        var recommendationType = (Type)recommendationState.Type;
        return GrainFactory.GetGrain(recommendationType, recommendationId, keyExtension: GetRecommendationKey()).AsReference<IRecommendation>();
    }

    RecommendationKey GetRecommendationKey()
    {
        this.GetPrimaryKey(out var keyAsString);
        var key = (RecommendationsManagerKey)keyAsString!;
        return new RecommendationKey(key.EventStore, key.Namespace);
    }
}
