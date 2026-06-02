// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Api.SequenceQueries;

/// <summary>
/// Represents the filter definition for a sequence query.
/// </summary>
/// <param name="EventSourceId">Optional event source id to filter by.</param>
/// <param name="EventTypes">Optional event type ids to filter by.</param>
/// <param name="StartTime">Optional start time to filter events from.</param>
/// <param name="EndTime">Optional end time to filter events to.</param>
public record SequenceQueryFilter(
    string? EventSourceId = default,
    IEnumerable<string>? EventTypes = default,
    DateTimeOffset? StartTime = default,
    DateTimeOffset? EndTime = default);
