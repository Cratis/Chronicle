// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands.ModelBound;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Grpc;

namespace Cratis.Chronicle.Recommendations;

/// <summary>
/// Represents the command for ignoring a recommendation.
/// </summary>
/// <param name="EventStore">The name of the event store the recommendation belongs to.</param>
/// <param name="Namespace">The namespace the recommendation belongs to.</param>
/// <param name="RecommendationId">The unique identifier of the recommendation to ignore.</param>
[Command]
[BelongsTo(WellKnownServices.Recommendations)]
public record IgnoreRecommendation(EventStoreName EventStore, EventStoreNamespaceName Namespace, Concepts.Recommendations.RecommendationId RecommendationId)
{
    /// <summary>
    /// Handles the command by asking the recommendations manager grain to ignore the recommendation.
    /// </summary>
    /// <param name="grainFactory">The <see cref="IGrainFactory"/> to get the recommendations manager grain with.</param>
    /// <returns>Awaitable task.</returns>
    public Task Handle(IGrainFactory grainFactory) =>
        grainFactory.GetRecommendationsManager(new EventStoreAndNamespace(EventStore, Namespace))
            .Ignore(RecommendationId);
}
