// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Recommendations;

namespace Cratis.Chronicle.Recommendations;

/// <summary>
/// Defines a system that manages recommendations.
/// </summary>
public interface IRecommendationsManager : IGrainWithIntegerCompoundKey
{
    /// <summary>
    /// Add a recommendation.
    /// </summary>
    /// <param name="description">The description of the recommendation.</param>
    /// <param name="request">The request for the recommendation.</param>
    /// <typeparam name="TRecommendation">Type of recommendation to add.</typeparam>
    /// <typeparam name="TRequest">Type of request for the recommendation.</typeparam>
    /// <returns>The <see cref="RecommendationId"/> for the added recommendation.</returns>
    Task<RecommendationId> Add<TRecommendation, TRequest>(RecommendationDescription description, TRequest request)
        where TRecommendation : IRecommendation<TRequest>
        where TRequest : class, IRecommendationRequest;

    /// <summary>
    /// Perform a recommendation.
    /// </summary>
    /// <param name="recommendationId">The <see cref="RecommendationId"/> to perform.</param>
    /// <returns>Awaitable task.</returns>
    Task Perform(RecommendationId recommendationId);

    /// <summary>
    /// Ignore a recommendation.
    /// </summary>
    /// <param name="recommendationId">The <see cref="RecommendationId"/> to ignore.</param>
    /// <returns>Awaitable task.</returns>
    Task Ignore(RecommendationId recommendationId);
}
