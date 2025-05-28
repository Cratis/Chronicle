// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Api.Recommendations;

/// <summary>
/// Represents information about a recommendation.
/// </summary>
/// <param name="Id">The unique identifier of the recommendation.</param>
/// <param name="Name">The name of the recommendation.</param>
/// <param name="Description">The details of the recommendation.</param>
/// <param name="Type">The type of the recommendation.</param>
/// <param name="Occurred">When the recommendation occurred.</param>
public record Recommendation(
    Guid Id,
    string Name,
    string Description,
    string Type,
    DateTimeOffset Occurred);
