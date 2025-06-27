// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Properties;

namespace Cratis.Chronicle.Projections;

/// <summary>
/// Represents the result of an projection.
/// </summary>
/// <param name="Model">The instance of the Model as <see cref="JsonObject"/>.</param>
/// <param name="AffectedProperties">Collection of properties that was set.</param>
/// <param name="ProjectedEventsCount">Number of events that caused projection.</param>
/// <param name="LastHandledEventSequenceNumber">The last handled event sequence number.</param>
public record ProjectionResultRaw(JsonObject Model, IEnumerable<PropertyPath> AffectedProperties, int ProjectedEventsCount, EventSequenceNumber LastHandledEventSequenceNumber);
