// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Aksio.Cratis.Recommendations;

/// <summary>
/// Represents the description for a recommendation.
/// </summary>
/// <param name="Value">The actual value.</param>
public record RecommendationDescription(string Value) : ConceptAs<string>(Value)
{
    /// <summary>
    /// The name of a job that is not set.
    /// </summary>
    public static readonly RecommendationDescription NotSet = "[Not set]";

    /// <summary>
    /// Implicitly convert from <see cref="string"/> to <see cref="RecommendationDescription"/>.
    /// </summary>
    /// <param name="value">String to convert from.</param>
    public static implicit operator RecommendationDescription(string value) => new(value);

    /// <summary>
    /// Implicitly convert from <see cref="RecommendationDescription"/> to <see cref="string"/>.
    /// </summary>
    /// <param name="value"><see cref="RecommendationDescription"/> to convert from.</param>
    public static implicit operator string(RecommendationDescription value) => value.Value;
}
