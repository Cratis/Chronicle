// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Recommendations;
using Cratis.Chronicle.Storage;
using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Recommendations;

/// <summary>
/// Represents an implementation of <see cref="IRecommendationsManager"/> that has a result.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="RecommendationsManager"/> class.
/// </remarks>
/// <param name="storage"><see cref="IStorage"/> for accessing underlying storage.</param>
/// <param name="logger">The logger.</param>
public class RecommendationsManager(IStorage storage, ILogger<RecommendationsManager> logger) : Grain, IRecommendationsManager
{
    RecommendationIndex? _index;

    /// <inheritdoc/>
    public async Task<RecommendationId> Add<TRecommendation, TRequest>(RecommendationDescription description, TRequest request)
        where TRecommendation : IRecommendation<TRequest>
        where TRequest : class, IRecommendationRequest
    {
        var key = GetRecommendationKey();
        var recommendationStorage = storage.GetEventStore(key.EventStore).GetNamespace(key.Namespace).Recommendations;
        var recommendationType = (RecommendationType)typeof(TRecommendation);
        var index = _index ??= new RecommendationIndex(recommendationStorage);
        var existing = await index.Find(recommendationType, request);
        if (existing is not null)
        {
            return existing;
        }

        var candidates = await index.CandidatesFor(request);
        var superseded = IgnoredRecommendations.SupersededBy(candidates, recommendationType, request).ToArray();
        var pruned = await IgnoredRecommendations.Remove(recommendationStorage, superseded);
        foreach (var row in superseded)
        {
            index.Remove(row.Id);
        }
        if (pruned > 0)
        {
            logger.PrunedIgnoredRecommendations(pruned);
        }

        var id = RecommendationId.New();
        var recommendation = GrainFactory.GetGrain<TRecommendation>(id, keyExtension: GetRecommendationKey());
        await recommendation.Initialize(description, request);
        index.Add(id, recommendationType, request);
        return id;
    }

    /// <inheritdoc/>
    public async Task Ignore(RecommendationId recommendationId)
    {
        var recommendation = await GetGrainFor(recommendationId);
        await recommendation.Ignore();
    }

    /// <inheritdoc/>
    public async Task Unignore(RecommendationId recommendationId)
    {
        var recommendation = await GetGrainFor(recommendationId);
        await recommendation.Unignore();
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
