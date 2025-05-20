// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Recommendations;

namespace Cratis.Chronicle.Api.Recommendations;

/// <summary>
/// Represents the API for working with recommendations.
/// </summary>
[Route("/api/event-store/{eventStore}/{namespace}/recommendations")]
public class RecommendationCommands : ControllerBase
{
    readonly IRecommendations _recommendations;

    /// <summary>
    /// Initializes a new instance of the <see cref="RecommendationCommands"/> class.
    /// </summary>
    /// <param name="recommendations"><see cref="IRecommendations"/> for recommendations.</param>
    internal RecommendationCommands(IRecommendations recommendations)
    {
        _recommendations = recommendations;
    }

    /// <summary>
    /// Perform a recommendation.
    /// </summary>
    /// <param name="eventStore">EventStore the recommendations are for.</param>
    /// <param name="namespace">Namespace the recommendations are for.</param>
    /// <param name="recommendationId">The unique identifier of the recommendation to perform.</param>
    /// <returns>Awaitable task.</returns>
    [HttpPost("{recommendationId}/perform")]
    public Task Perform(
        [FromRoute] string eventStore,
        [FromRoute] string @namespace,
        [FromRoute] Guid recommendationId) =>
        _recommendations.Perform(new() { EventStore = eventStore, Namespace = @namespace, RecommendationId = recommendationId });

    /// <summary>
    /// Ignore a recommendation.
    /// </summary>
    /// <param name="eventStore">EventStore the recommendations are for.</param>
    /// <param name="namespace">Namespace the recommendations are for.</param>
    /// <param name="recommendationId">The unique identifier of the recommendation to perform.</param>
    /// <returns>Awaitable task.</returns>
    [HttpPost("{recommendationId}/ignore")]
    public Task Ignore(
        [FromRoute] string eventStore,
        [FromRoute] string @namespace,
        [FromRoute] Guid recommendationId) =>
        _recommendations.Ignore(new() { EventStore = eventStore, Namespace = @namespace, RecommendationId = recommendationId });
}
