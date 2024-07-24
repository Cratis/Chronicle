// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Chronicle.Grains.Projections;
using Cratis.Json;

namespace Cratis.Chronicle.Services.Projections;

/// <summary>
/// Extension methods for converting to and from <see cref="ProjectionResult"/>.
/// </summary>
public static class ProjectionResultConverters
{
    /// <summary>
    /// Convert to contract representation.
    /// </summary>
    /// <param name="result"><see cref="ProjectionResult"/> to convert from.</param>
    /// <returns>Converted <see cref="Contracts.Projections.ProjectionResult"/>.</returns>
    public static Contracts.Projections.ProjectionResult ToContract(this ProjectionResult result) =>
        new()
        {
            Model = JsonSerializer.Serialize(result.Model, Globals.JsonSerializerOptions),
            AffectedProperties = result.AffectedProperties.Select(_ => _.ToString()).ToList(),
            ProjectedEventsCount = result.ProjectedEventsCount
        };
}
