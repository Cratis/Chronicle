// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Aksio.Cratis.Recommendations;

namespace Aksio.Cratis.Kernel.Storage.Recommendations;

/// <summary>
/// Defines the storage for <see cref="RecommendationState"/>.
/// </summary>
public interface IRecommendationStorage
{
    /// <summary>
    /// Get a recommendation by its <see cref="RecommendationId"/>.
    /// </summary>
    /// <param name="recommendationId">The <see cref="RecommendationId"/> to get for.</param>
    /// <returns>An <see cref="RecommendationState"/> if it was found, null if not.</returns>
    Task<RecommendationState?> Get(RecommendationId recommendationId);

    /// <summary>
    /// Save a recommendation.
    /// </summary>
    /// <param name="recommendationId">The <see cref="RecommendationId"/> to save for.</param>
    /// <param name="recommendationState">The <see cref="RecommendationState"/> to save.</param>
    /// <returns>Awaitable task.</returns>
    Task Save(RecommendationId recommendationId, RecommendationState recommendationState);

    /// <summary>
    /// Remove a recommendation.
    /// </summary>
    /// <param name="recommendationId">The <see cref="RecommendationId"/> of the recommendation to remove.</param>
    /// <returns>Awaitable task.</returns>
    Task Remove(RecommendationId recommendationId);

    /// <summary>
    /// Get all recommendations.
    /// </summary>
    /// <returns>A collection of <see cref="RecommendationState"/>.</returns>
    Task<IImmutableList<RecommendationState>> GeAll();

    /// <summary>
    /// Observe all recommendations.
    /// </summary>
    /// <returns>An observable of collection of recommendations.</returns>
    IObservable<IEnumerable<RecommendationState>> ObserveRecommendations();
}
