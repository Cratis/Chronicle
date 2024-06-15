// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Recommendations;

namespace Cratis.Chronicle.Storage.Recommendations;

/// <summary>
/// Holds the state of a recommendation.
/// </summary>
public class RecommendationState
{
    /// <summary>
    /// Gets or sets the <see cref="RecommendationId"/>.
    /// </summary>
    public RecommendationId Id { get; set; } = RecommendationId.NotSet;

    /// <summary>
    /// Gets or sets the <see cref="RecommendationName"/>.
    /// </summary>
    public RecommendationName Name { get; set; } = RecommendationName.NotSet;

    /// <summary>
    /// Gets or sets the <see cref="RecommendationDescription"/>.
    /// </summary>
    public RecommendationDescription Description { get; set; } = RecommendationDescription.NotSet;

    /// <summary>
    /// Gets or sets the <see cref="RecommendationType"/>.
    /// </summary>
    public RecommendationType Type { get; set; } = RecommendationType.NotSet;

    /// <summary>
    /// Gets or sets when the recommendation occurred.
    /// </summary>
    public DateTimeOffset Occurred { get; set; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// Gets or sets the request associated with the recommendation.
    /// </summary>
    public object Request { get; set; } = default!;
}
