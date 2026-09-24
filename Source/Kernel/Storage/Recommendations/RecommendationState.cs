// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Recommendations;

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
    public IRecommendationRequest Request { get; set; } = default!;

    /// <summary>
    /// Gets or sets a value indicating whether a human has declined this recommendation.
    /// </summary>
    /// <remarks>
    /// An ignored recommendation is kept rather than deleted, because the decision to decline it is
    /// what has to survive. Recommendations are raised from a pure evaluation of current state, so a
    /// deleted one is simply re-raised the next time that evaluation runs - which is every client
    /// reconnect. Keeping it means the duplicate check in the recommendations manager recognizes it
    /// and declines to raise it again, while a genuinely changed situation - which produces a
    /// different request, and so a different identity - is raised as the new recommendation it is.
    /// </remarks>
    public bool IsIgnored { get; set; }
}
