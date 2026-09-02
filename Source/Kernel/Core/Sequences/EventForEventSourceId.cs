// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;

namespace Cratis.Chronicle.Sequences;

/// <summary>
/// Represents the payload for appending an event to a specific event source, as part of a batch spanning several
/// event sources.
/// </summary>
/// <param name="EventSourceId">The event source the event belongs to.</param>
/// <param name="EventSourceType">The type of event source.</param>
/// <param name="EventStreamType">The stream type within the event source.</param>
/// <param name="EventStreamId">The stream within the stream type.</param>
/// <param name="EventType">The type of event being appended.</param>
/// <param name="Content">The content of the event.</param>
/// <param name="Tags">The tags to associate with the event.</param>
/// <param name="Occurred">Optional occurred time. If null, the server sets it to approximately the time of append.</param>
/// <param name="Subject">Optional subject identifying the compliance target for the event. Defaults to the event source.</param>
public record EventForEventSourceId(
    string EventSourceId,
    string EventSourceType,
    string EventStreamType,
    string EventStreamId,
    EventType EventType,
    JsonObject Content,
    IEnumerable<string>? Tags = default,
    DateTimeOffset? Occurred = default,
    string? Subject = default);
