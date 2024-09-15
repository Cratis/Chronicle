// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reactive.Subjects;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Recommendations;
using Cratis.Chronicle.Reactive;
using Cratis.Chronicle.Storage;
using Cratis.Chronicle.Storage.Recommendations;

namespace Cratis.Api.Recommendations;

/// <summary>
/// Represents the API for working with recommendations.
/// </summary>
/// <param name="storage"><see cref="IStorage"/> for recommendations.</param>
[Route("/api/event-store/{eventStore}/{namespace}/recommendations")]
public class RecommendationQueries(IStorage storage) : ControllerBase
{
    /// <summary>
    /// Get all observers for an event store and namespace.
    /// </summary>
    /// <param name="eventStore"><see cref="EventStoreName"/> the recommendations are for.</param>
    /// <param name="namespace"><see cref="EventStoreNamespaceName"/> the recommendations are for.</param>
    /// <returns>Collection of <see cref="RecommendationInformation"/>.</returns>
    [HttpGet]
    public async Task<IEnumerable<RecommendationInformation>> GetRecommendations(
        [FromRoute] EventStoreName eventStore,
        [FromRoute] EventStoreNamespaceName @namespace)
    {
        var namespaceStorage = storage.GetEventStore(eventStore).GetNamespace(@namespace);
        var recommendations = await namespaceStorage.Recommendations.GetAll();
        return recommendations.Select(_ =>
        {
            return new RecommendationInformation(
                _.Id,
                _.Name,
                _.Description,
                _.Type,
                _.Occurred);
        }).ToArray();
    }

    /// <summary>
    /// Get and observe all observers for an event store and namespace.
    /// </summary>
    /// <param name="eventStore"><see cref="EventStoreName"/> the recommendations are for.</param>
    /// <param name="namespace"><see cref="EventStoreNamespaceName"/> the recommendations are for.</param>
    /// <returns>An observable of a collection of <see cref="RecommendationInformation"/>.</returns>
    [HttpGet("all-recommendations/observe")]
    public async Task<ISubject<IEnumerable<RecommendationInformation>>> AllRecommendations(
        [FromRoute] EventStoreName eventStore,
        [FromRoute] EventStoreNamespaceName @namespace)
    {
        var namespaceStorage = storage.GetEventStore(eventStore).GetNamespace(@namespace);
        var recommendations = namespaceStorage.Recommendations.ObserveRecommendations();

        return new TransformingSubject<IEnumerable<RecommendationState>, IEnumerable<RecommendationInformation>>(
            recommendations,
            recommendations => recommendations.Select(_ =>
            {
                return new RecommendationInformation(
                    _.Id,
                    _.Name,
                    _.Description,
                    _.Type,
                    _.Occurred);
            }).ToArray());
    }
}
