// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;
using Cratis.Properties;

namespace Cratis.Kernel.Grains.Projections;

/// <summary>
/// Represents the result of an <see cref="IImmediateProjection"/>.
/// </summary>
/// <param name="Model">The Json representation of the model.</param>
/// <param name="AffectedProperties">Collection of properties that was set.</param>
/// <param name="ProjectedEventsCount">Number of events that caused projection.</param>
public record ImmediateProjectionResult(JsonObject Model, IEnumerable<PropertyPath> AffectedProperties, int ProjectedEventsCount)
{
    /// <summary>
    /// Represents an empty <see cref="ImmediateProjectionResult"/>.
    /// </summary>
    public static readonly ImmediateProjectionResult Empty = new([], Array.Empty<PropertyPath>(), 0);
}
