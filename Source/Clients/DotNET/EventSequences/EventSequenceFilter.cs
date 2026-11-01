// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.EventSequences;

/// <summary>
/// Optional filters applied in storage before ordering and limiting a page.
/// </summary>
/// <param name="EventSourceId">The event source identifier, or null to include all sources.</param>
/// <param name="EventSourceType">The event source type, or null to include all source types.</param>
/// <param name="EventStreamType">The stream type, or null to include all stream types.</param>
/// <param name="EventStreamId">The stream identifier, or null to include all streams.</param>
/// <param name="EventTypes">The event types to include, or null or empty for all types.</param>
/// <param name="Tags">Tags to match any of, or null or empty for no tag restriction.</param>
public record EventSequenceFilter(
    EventSourceId? EventSourceId = default,
    EventSourceType? EventSourceType = default,
    EventStreamType? EventStreamType = default,
    EventStreamId? EventStreamId = default,
    IEnumerable<EventType>? EventTypes = default,
    IEnumerable<Tag>? Tags = default);
