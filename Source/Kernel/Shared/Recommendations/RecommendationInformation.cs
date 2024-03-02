// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Aksio.Cratis.Recommendations;

/// <summary>
/// Represents information about an recommendation.
/// </summary>
/// <param name="Id">The unique identifier of the recommendation.</param>
/// <param name="Name">The name of the recommendation.</param>
/// <param name="Description">The details of the recommendation.</param>
/// <param name="Type">The type of the recommendation.</param>
/// <param name="Occurred">When the recommendation occurred.</param>
public record RecommendationInformation(
    RecommendationId Id,
    RecommendationName Name,
    RecommendationDescription Description,
    RecommendationType Type,
    DateTimeOffset Occurred);
