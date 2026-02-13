// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;
using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.Projections;

/// <summary>
/// Represents the result of an projection.
/// </summary>
/// <param name="ReadModel">The instance of the read model as <see cref="JsonObject"/>.</param>
/// <param name="ProjectedEventsCount">Number of events that caused projection.</param>
/// <param name="LastHandledEventSequenceNumber">The last handled event sequence number.</param>
public record ProjectionResultRaw(JsonObject ReadModel, int ProjectedEventsCount, EventSequenceNumber LastHandledEventSequenceNumber);
