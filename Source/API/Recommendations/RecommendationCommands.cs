// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Kernel.Grains.Recommendations;
using Cratis.Recommendations;
using Microsoft.AspNetCore.Mvc;

namespace Cratis.API.Recommendations;

/// <summary>
/// Represents the API for working with recommendations.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="RecommendationCommands"/> class.
/// </remarks>
/// <param name="grainFactory"><see cref="IGrainFactory"/> for getting grains.</param>
[Route("/api/events/store/{eventStore}/{namespace}/recommendations")]
public class RecommendationCommands(IGrainFactory grainFactory) : ControllerBase
{
    /// <summary>
    /// Perform a recommendation.
    /// </summary>
    /// <param name="eventStore">The <see cref="EventStoreName"/> for the recommendation.</param>
    /// <param name="namespace">The <see cref="EventStoreNamespaceName"/> for the recommendation.</param>
    /// <param name="recommendationId">The <see cref="RecommendationId"/> of the recommendation to perform.</param>
    /// <returns>Awaitable task.</returns>
    [HttpPost("{recommendationId}/perform")]
    public async Task Perform(
        [FromRoute] EventStoreName eventStore,
        [FromRoute] EventStoreNamespaceName @namespace,
        [FromRoute] RecommendationId recommendationId)
    {
        await GetRecommendationsManager(eventStore, @namespace).Perform(recommendationId);
    }

    /// <summary>
    /// Ignore a recommendation.
    /// </summary>
    /// <param name="eventStore">The <see cref="EventStoreName"/> for the recommendation.</param>
    /// <param name="namespace">The <see cref="EventStoreNamespaceName"/> for the recommendation.</param>
    /// <param name="recommendationId">The <see cref="RecommendationId"/> of the recommendation to ignore.</param>
    /// <returns>Awaitable task.</returns>
    [HttpPost("{recommendationId}/ignore")]
    public async Task Ignore(
        [FromRoute] EventStoreName eventStore,
        [FromRoute] EventStoreNamespaceName @namespace,
        [FromRoute] RecommendationId recommendationId)
    {
        await GetRecommendationsManager(eventStore, @namespace).Ignore(recommendationId);
    }

    IRecommendationsManager GetRecommendationsManager(EventStoreName eventStore, EventStoreNamespaceName @namespace) =>
        grainFactory.GetGrain<IRecommendationsManager>(0, new RecommendationsManagerKey(eventStore, @namespace));
}
