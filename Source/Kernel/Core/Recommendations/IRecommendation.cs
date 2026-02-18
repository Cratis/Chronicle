// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Recommendations;

namespace Cratis.Chronicle.Recommendations;

/// <summary>
/// Defines a system for a recommendation in the system.
/// </summary>
public interface IRecommendation : IGrainWithGuidCompoundKey
{
    /// <summary>
    /// Perform the recommendation.
    /// </summary>
    /// <returns>Awaitable task.</returns>
    Task Perform();

    /// <summary>
    /// Ignore the recommendation.
    /// </summary>
    /// <returns>Awaitable task.</returns>
    Task Ignore();
}

/// <summary>
/// Defines a system for a recommendation in the system.
/// </summary>
/// <typeparam name="TRequest">Type of request for the task.</typeparam>
public interface IRecommendation<TRequest> : IRecommendation
    where TRequest : class, IRecommendationRequest
{
    /// <summary>
    /// Initialize the recommendation.
    /// </summary>
    /// <param name="description">The description of the recommendation.</param>
    /// <param name="request">Request to set.</param>
    /// <returns>Awaitable task.</returns>
    Task Initialize(RecommendationDescription description, TRequest request);
}
