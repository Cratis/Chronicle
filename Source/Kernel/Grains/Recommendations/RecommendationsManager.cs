// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Kernel.Storage;
using Aksio.Cratis.Recommendations;

namespace Aksio.Cratis.Kernel.Grains.Recommendations;

/// <summary>
/// Represents an implementation of <see cref="IRecommendationsManager"/> that has a result.
/// </summary>
public class RecommendationsManager : Grain, IRecommendationsManager
{
    readonly IStorage _storage;

    /// <summary>
    /// Initializes a new instance of the <see cref="RecommendationsManager"/> class.
    /// </summary>
    /// <param name="storage"><see cref="IStorage"/> for accessing underlying storage.</param>
    public RecommendationsManager(IStorage storage)
    {
        _storage = storage;
    }

    /// <inheritdoc/>
    public async Task<RecommendationId> Add<TRecommendation, TRequest>(RecommendationDescription description, TRequest request)
        where TRecommendation : IRecommendation<TRequest>
        where TRequest : class
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
        var recommendationStorage = _storage.GetEventStore((string)key.MicroserviceId).GetNamespace(key.TenantId).Recommendations;
        var recommendationState = await recommendationStorage.Get(recommendationId) ?? throw new UnknownRecommendation(key.MicroserviceId, key.TenantId, recommendationId);
        var recommendationType = (Type)recommendationState.Type;
        return GrainFactory.GetGrain(recommendationType, recommendationId, keyExtension: GetRecommendationKey()).AsReference<IRecommendation>();
    }

    RecommendationKey GetRecommendationKey()
    {
        this.GetPrimaryKey(out var keyAsString);
        var key = (RecommendationsManagerKey)keyAsString;
        return new RecommendationKey(key.MicroserviceId, key.TenantId);
    }
}
