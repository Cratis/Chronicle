// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Recommendations;

/// <summary>
/// Represents the name of a recommendation.
/// </summary>
/// <param name="Value">The actual value.</param>
public record RecommendationName(string Value) : ConceptAs<string>(Value)
{
    /// <summary>
    /// The name of a job that is not set.
    /// </summary>
    public static readonly RecommendationName NotSet = "[Not set]";

    /// <summary>
    /// Implicitly convert from <see cref="string"/> to <see cref="RecommendationName"/>.
    /// </summary>
    /// <param name="value">String to convert from.</param>
    public static implicit operator RecommendationName(string value) => new(value);

    /// <summary>
    /// Implicitly convert from <see cref="RecommendationName"/> to <see cref="string"/>.
    /// </summary>
    /// <param name="value"><see cref="RecommendationName"/> to convert from.</param>
    public static implicit operator string(RecommendationName value) => value.Value;
}
