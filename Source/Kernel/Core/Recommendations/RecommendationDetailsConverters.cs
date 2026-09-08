// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Recommendations;

namespace Cratis.Chronicle.Recommendations;

/// <summary>
/// Converts stored recommendations into the recommendation read model.
/// </summary>
/// <remarks>
/// These live beside the read model rather than on it because a static method on a <c>[ReadModel]</c> whose return
/// shape is a supported query shape becomes a generated query proxy and an HTTP endpoint - accessibility is not
/// what the proxy generator looks at. A conversion helper is not an operation anybody should be able to call.
/// </remarks>
internal static class RecommendationDetailsConverters
{
    /// <summary>
    /// Converts stored recommendations into read models, materializing the result.
    /// </summary>
    /// <param name="recommendations">The stored recommendations.</param>
    /// <returns>The recommendations as read models.</returns>
    /// <remarks>
    /// Materialized, because what leaves here is serialized from its runtime type and a lazily projected sequence
    /// keeps the source type as its first generic argument. See EventStoreNames for what that cost the last time.
    /// </remarks>
    internal static IEnumerable<RecommendationDetails> ToDetails(this IEnumerable<RecommendationState> recommendations) =>
        [.. recommendations.Select(ToDetails)];

    /// <summary>
    /// Converts a stored recommendation into a read model.
    /// </summary>
    /// <param name="recommendation">The stored recommendation.</param>
    /// <returns>The recommendation as a read model.</returns>
    internal static RecommendationDetails ToDetails(this RecommendationState recommendation) =>
        new(
            recommendation.Id,
            recommendation.Name,
            recommendation.Description,
            recommendation.Type,
            recommendation.Occurred);
}
