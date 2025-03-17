// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reactive.Subjects;
using Cratis.Chronicle.Api.EventStores;
using Cratis.Chronicle.Contracts.Recommendations;
using Cratis.Chronicle.Reactive;

namespace Cratis.Chronicle.Api.Recommendations;

/// <summary>
/// Represents the API for working with recommendations.
/// </summary>
/// <param name="recommendations"><see cref="IRecommendations"/> for recommendations.</param>
[Route("/api/event-store/{eventStore}/{namespace}/recommendations")]
public class RecommendationQueries(IRecommendations recommendations) : ControllerBase
{
    /// <summary>
    /// Get all observers for an event store and namespace.
    /// </summary>
    /// <param name="eventStore">EventStore the recommendations are for.</param>
    /// <param name="namespace">Namespace the recommendations are for.</param>
    /// <returns>Collection of <see cref="Recommendation"/>.</returns>
    [HttpGet]
    public Task<IEnumerable<Recommendation>> GetRecommendations(
        [FromRoute] string eventStore,
        [FromRoute] string @namespace) =>
        recommendations.GetRecommendations(new() { EventStore = eventStore, Namespace = @namespace });

    /// <summary>
    /// Get and observe all observers for an event store and namespace.
    /// </summary>
    /// <param name="eventStore">EventStore the recommendations are for.</param>
    /// <param name="namespace">Namespace the recommendations are for.</param>
    /// <returns>An observable of a collection of <see cref="Recommendation"/>.</returns>
    [HttpGet("all-recommendations/observe")]
    public ISubject<IEnumerable<Recommendation>> AllRecommendations(
        [FromRoute] string eventStore,
        [FromRoute] string @namespace) =>
        recommendations.InvokeAndWrapWithSubject(token => recommendations.ObserveRecommendations(new() { EventStore = eventStore, Namespace = @namespace }, token));
}
