// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Applications.Queries;
using Cratis.Chronicle;
using Cratis.Chronicle.Recommendations;
using Microsoft.AspNetCore.Mvc;

namespace Cratis.API.Recommendations;

/// <summary>
/// Represents the API for working with recommendations.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="RecommendationQueries"/> class.
/// </remarks>
[Route("/api/events/store/{eventStore}/{namespace}/recommendations")]
public class RecommendationQueries() : ControllerBase
{
    /// <summary>
    /// Get all observers for an event store and namespace.
    /// </summary>
    /// <param name="eventStore"><see cref="EventStoreName"/> the recommendations are for.</param>
    /// <param name="namespace"><see cref="EventStoreNamespaceName"/> the recommendations are for.</param>
    /// <returns>Collection of <see cref="RecommendationInformation"/>.</returns>
    [HttpGet]
    public Task<IEnumerable<RecommendationInformation>> GetRecommendations(
        [FromRoute] EventStoreName eventStore,
        [FromRoute] EventStoreNamespaceName @namespace)
    {
        throw new NotImplementedException();
    }

    /// <summary>
    /// Get and observe all observers for an event store and namespace.
    /// </summary>
    /// <param name="eventStore"><see cref="EventStoreName"/> the recommendations are for.</param>
    /// <param name="namespace"><see cref="EventStoreNamespaceName"/> the recommendations are for.</param>
    /// <returns>Client observable of a collection of <see cref="RecommendationInformation"/>.</returns>
    [HttpGet("observe")]
    public Task<ClientObservable<IEnumerable<RecommendationInformation>>> AllRecommendations(
        [FromRoute] EventStoreName eventStore,
        [FromRoute] EventStoreNamespaceName @namespace)
    {
        throw new NotImplementedException();
    }
}
