// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;
using Cratis.Chronicle.Properties;

namespace Cratis.Chronicle.Grains.Projections;

/// <summary>
/// Represents the result of an <see cref="IImmediateProjection"/>.
/// </summary>
/// <param name="Model">The Json representation of the model.</param>
/// <param name="AffectedProperties">Collection of properties that was set.</param>
/// <param name="ProjectedEventsCount">Number of events that caused projection.</param>
public record ProjectionResult(JsonObject Model, IEnumerable<PropertyPath> AffectedProperties, int ProjectedEventsCount)
{
    /// <summary>
    /// Represents an empty <see cref="ProjectionResult"/>.
    /// </summary>
    public static readonly ProjectionResult Empty = new([], [], 0);
}
