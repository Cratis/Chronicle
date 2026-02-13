// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;
using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.Grains.EventSequences;

/// <summary>
/// Represents the payload for appending an event.
/// </summary>
/// <param name="EventSourceType">The <see cref="EventSourceType">event source</see> to append to.</param>
/// <param name="EventSourceId">The <see cref="EventSourceId"/> to append to.</param>
/// <param name="eventStreamType">the <see cref="EventStreamType"/> to append to.</param>
/// <param name="eventStreamId">The <see cref="EventStreamId"/> to append to.</param>
/// <param name="EventType">The <see cref="EventType">type of event</see> to append.</param>
/// <param name="Tags">Collection of tags associated with the event.</param>
/// <param name="Content">The JSON payload of the event.</param>
public record EventToAppend(
    EventSourceType EventSourceType,
    EventSourceId EventSourceId,
    EventStreamType eventStreamType,
    EventStreamId eventStreamId,
    EventType EventType,
    IEnumerable<Tag> Tags,
    JsonObject Content);
