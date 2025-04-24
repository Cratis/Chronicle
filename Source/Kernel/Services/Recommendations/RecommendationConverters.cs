// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Recommendations;
using Cratis.Chronicle.Storage.Recommendations;

namespace Cratis.Chronicle.Services.Recommendations;

/// <summary>
/// Converts between <see cref="Concepts.Recommendations.RecommendationInformation"/> and <see cref="Recommendation"/>.
/// </summary>
internal static class RecommendationConverters
{
    /// <summary>
    /// Convert from <see cref="Concepts.Recommendations.RecommendationInformation"/> to <see cref="Recommendation"/>.
    /// </summary>
    /// <param name="recommendations">Collection of <see cref="Concepts.Recommendations.RecommendationInformation"/> to convert from.</param>
    /// <returns>Converted collection of <see cref="Recommendation"/>.</returns>
    public static IEnumerable<Recommendation> ToContract(this IEnumerable<Concepts.Recommendations.RecommendationInformation> recommendations) =>
        recommendations.Select(_ => _.ToContract());

    /// <summary>
    /// Convert from <see cref="Concepts.Recommendations.RecommendationInformation"/> to <see cref="Recommendation"/>.
    /// </summary>
    /// <param name="recommendation"><see cref="Concepts.Recommendations.RecommendationInformation"/> to convert from.</param>
    /// <returns>Converted <see cref="Recommendation"/>.</returns>
    public static Recommendation ToContract(this Concepts.Recommendations.RecommendationInformation recommendation)
    {
        return new()
        {
            Id = recommendation.Id,
            Name = recommendation.Name,
            Description = recommendation.Description,
            Type = recommendation.Type,
            Occurred = recommendation.Occurred!
        };
    }

    /// <summary>
    /// Convert from <see cref="RecommendationState"/> to <see cref="Recommendation"/>.
    /// </summary>
    /// <param name="recommendations">Collection of <see cref="RecommendationState"/> to convert from.</param>
    /// <returns>Converted collection of <see cref="Recommendation"/>.</returns>
    public static IEnumerable<Recommendation> ToContract(this IEnumerable<RecommendationState> recommendations) =>
        recommendations.Select(_ => _.ToContract());

    /// <summary>
    /// Convert from <see cref="RecommendationState"/> to <see cref="Recommendation"/>.
    /// </summary>
    /// <param name="recommendation"><see cref="RecommendationState"/> to convert from.</param>
    /// <returns>Converted <see cref="Recommendation"/>.</returns>
    public static Recommendation ToContract(this RecommendationState recommendation)
    {
        return new()
        {
            Id = recommendation.Id,
            Name = recommendation.Name,
            Description = recommendation.Description,
            Type = recommendation.Type,
            Occurred = recommendation.Occurred!
        };
    }
}
