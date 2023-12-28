// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;
using Aksio.Cratis.Properties;

namespace Aksio.Cratis.Projections;

/// <summary>
/// Represents the result of an immediate projection.
/// </summary>
/// <param name="Model">The instance of the Model as <see cref="JsonObject"/>.</param>
/// <param name="AffectedProperties">Collection of properties that was set.</param>
/// <param name="ProjectedEventsCount">Number of events that caused projection.</param>
public record ImmediateProjectionResultRaw(JsonObject Model, IEnumerable<PropertyPath> AffectedProperties, int ProjectedEventsCount);
