// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Sequences;

/// <summary>
/// An event belonging to a specific source in a named-tag batch.
/// </summary>
/// <param name="EventSourceId">The event source id.</param>
/// <param name="EventSourceType">The event source type.</param>
/// <param name="EventStreamType">The stream type.</param>
/// <param name="EventStreamId">The stream id.</param>
/// <param name="EventType">The event type.</param>
/// <param name="Content">The serialized content.</param>
/// <param name="Tags">The legacy tags.</param>
/// <param name="NamedTags">The named tags belonging to this event.</param>
/// <param name="Occurred">The optional occurrence time.</param>
/// <param name="Subject">The optional subject.</param>
public record EventForEventSourceIdWithNamedTags(
    string EventSourceId,
    string EventSourceType,
    string EventStreamType,
    string EventStreamId,
    EventType EventType,
    string Content,
    IEnumerable<string>? Tags,
    IEnumerable<NamedTag> NamedTags,
    DateTimeOffset? Occurred = default,
    string? Subject = default);
