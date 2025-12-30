// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Chronicle.Grains.Projections;

namespace Cratis.Chronicle.Services.Projections;

/// <summary>
/// Extension methods for converting to and from <see cref="ProjectionResult"/>.
/// </summary>
internal static class ProjectionResultConverters
{
    /// <summary>
    /// Convert to contract representation.
    /// </summary>
    /// <param name="result"><see cref="ProjectionResult"/> to convert from.</param>
    /// <param name="jsonSerializerOptions">Options for JSON serialization.</param>
    /// <returns>Converted <see cref="Contracts.Projections.ProjectionResult"/>.</returns>
    public static Contracts.Projections.ProjectionResult ToContract(this ProjectionResult result, JsonSerializerOptions jsonSerializerOptions) =>
        new()
        {
            ReadModel = JsonSerializer.Serialize(result.ReadModel, jsonSerializerOptions),
            ProjectedEventsCount = result.ProjectedEventsCount,
            LastHandledEventSequenceNumber = result.LastHandledEventSequenceNumber.Value
        };
}
