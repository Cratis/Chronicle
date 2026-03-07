// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Recommendations;
using Cratis.Chronicle.Storage.Recommendations;
using Orleans.Core;
using Orleans.TestKit;
using Orleans.TestKit.Storage;

namespace Cratis.Chronicle.Recommendations.for_Recommendation.given;

public class all_dependencies : Specification
{
    protected TestKitSilo silo = new();
    protected RecommendationId recommendationId;
    protected RecommendationKey recommendationKey;
    protected RecommendationDescription description;
    protected TestRecommendation<TheRequest> recommendation;
    protected TestStorageStats storageStats;
    protected IStorage<RecommendationState> storage;

    async Task Establish()
    {
        recommendationId = RecommendationId.NotSet;
        recommendationKey = RecommendationKey.NotSet;
        description = RecommendationDescription.NotSet;

        recommendation = await GetRecommendation<TheRequest>(recommendationId, recommendationKey);
        storageStats = GetTestStorageStatsFor<TheRequest>();
        storage = GetStorageFor<TheRequest>();
    }

    void Destroy() => ResetTestStorageStatsFor<TheRequest>();

    protected Task<TestRecommendation<TRequest>> GetRecommendation<TRequest>(RecommendationId id, RecommendationKey key)
        where TRequest : class, IRecommendationRequest => silo.CreateGrainAsync<TestRecommendation<TRequest>>(id, key);

    protected TestStorageStats? GetTestStorageStatsFor<TRequest>()
        where TRequest : class, IRecommendationRequest => silo.StorageStats<TestRecommendation<TRequest>, RecommendationState>();

    protected void ResetTestStorageStatsFor<TRequest>()
        where TRequest : class, IRecommendationRequest => GetTestStorageStatsFor<TRequest>()?.ResetCounts();

    protected IStorage<RecommendationState> GetStorageFor<TRequest>()
        where TRequest : class, IRecommendationRequest
        => silo.StorageManager.GetStorage<RecommendationState>(typeof(TestRecommendation<TRequest>).FullName);
}
