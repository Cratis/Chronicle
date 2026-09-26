// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reactive.Subjects;
using Cratis.Arc.Queries.ModelBound;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Grpc;
using Cratis.Chronicle.Storage;

namespace Cratis.Chronicle.Recommendations;

/// <summary>
/// Represents the read model for a recommendation, providing query access to the recommendation store.
/// </summary>
/// <param name="Id">The unique identifier of the recommendation.</param>
/// <param name="Name">The name of the recommendation.</param>
/// <param name="Description">The details of the recommendation.</param>
/// <param name="Type">The type of the recommendation.</param>
/// <param name="Occurred">When the recommendation occurred.</param>
/// <remarks>
/// Named for what it carries rather than for the concept, because the recommendation grain in this namespace
/// already owns that name.
/// </remarks>
[ReadModel]
[BelongsTo(WellKnownServices.Recommendations)]
public record RecommendationDetails(
    Guid Id,
    string Name,
    string Description,
    string Type,
    DateTimeOffset Occurred)
{
    /// <summary>
    /// Gets all recommendations for the given event store and namespace.
    /// </summary>
    /// <param name="eventStore">Name of the event store the recommendations are for.</param>
    /// <param name="namespace">Namespace within the event store the recommendations are for.</param>
    /// <param name="storage">The <see cref="IStorage"/> to read recommendations from.</param>
    /// <returns>A collection of recommendations.</returns>
    internal static async Task<IEnumerable<RecommendationDetails>> GetRecommendations(EventStoreName eventStore, EventStoreNamespaceName @namespace, IStorage storage)
    {
        var recommendations = await storage.GetEventStore(eventStore).GetNamespace(@namespace).Recommendations.GetAll();
        return recommendations.ToDetails();
    }

    /// <summary>
    /// Observes all recommendations for the given event store and namespace.
    /// </summary>
    /// <param name="eventStore">Name of the event store the recommendations are for.</param>
    /// <param name="namespace">Namespace within the event store the recommendations are for.</param>
    /// <param name="storage">The <see cref="IStorage"/> to observe recommendations from.</param>
    /// <returns>An observable subject emitting collections of recommendations.</returns>
    internal static ISubject<IEnumerable<RecommendationDetails>> AllRecommendations(EventStoreName eventStore, EventStoreNamespaceName @namespace, IStorage storage) =>
        storage
            .GetEventStore(eventStore)
            .GetNamespace(@namespace).Recommendations
            .ObserveRecommendations()
            .TransformSubject(_ => _.ToDetails());
}
