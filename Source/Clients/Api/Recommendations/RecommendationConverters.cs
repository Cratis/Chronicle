// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Api.Recommendations;

/// <summary>
/// Converters between <see cref="Contracts.Recommendations.Recommendation"/> and <see cref="Recommendation"/>.
/// </summary>
internal static class RecommendationConverters
{
    /// <summary>
    /// Converts a <see cref="Contracts.Recommendations.Recommendation"/> to a <see cref="Recommendation"/> for the API.
    /// </summary>
    /// <param name="recommendation">The recommendation to convert.</param>
    /// <returns>The converted recommendation.</returns>
    public static Recommendation ToApi(this Contracts.Recommendations.Recommendation recommendation) => new(
        recommendation.Id,
        recommendation.Name,
        recommendation.Description,
        recommendation.Type,
        recommendation.Occurred);

    /// <summary>
    /// Converts a collection of <see cref="Contracts.Recommendations.Recommendation"/> to a collection of <see cref="Recommendation"/> for the API.
    /// </summary>
    /// <param name="recommendations">The recommendations to convert.</param>
    /// <returns>The converted recommendations.</returns>
    public static IEnumerable<Recommendation> ToApi(this IEnumerable<Contracts.Recommendations.Recommendation> recommendations) =>
        recommendations.Select(ToApi);
}
