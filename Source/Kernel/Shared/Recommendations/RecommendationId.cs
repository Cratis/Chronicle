// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Aksio.Cratis.Recommendations;

/// <summary>
/// Represents the unique identifier of an recommendation.
/// </summary>
/// <param name="Value">Inner value.</param>
public record RecommendationId(Guid Value) : ConceptAs<Guid>(Value)
{
    /// <summary>
    /// Represents the "not set" <see cref="RecommendationId"/>.
    /// </summary>
    public static readonly RecommendationId NotSet = Guid.Empty;

    /// <summary>
    /// Implicitly convert from <see cref="Guid"/> to <see cref="RecommendationId"/>.
    /// </summary>
    /// <param name="id"><see cref="Guid"/> representation.</param>
    public static implicit operator RecommendationId(Guid id) => new(id);

    /// <summary>
    /// Create a new <see cref="RecommendationId"/>.
    /// </summary>
    /// <returns>A new <see cref="RecommendationId"/>.</returns>
    public static RecommendationId New() => new(Guid.NewGuid());
}
