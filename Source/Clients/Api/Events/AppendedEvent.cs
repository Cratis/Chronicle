// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Api.Events;

/// <summary>
/// Represents an event that has been appended to an event log.
/// </summary>
/// <param name="Context">The context for the event.</param>
/// <param name="Content">The JSON representation content of the event.</param>
/// <param name="OriginalContent">The original JSON content before any revisions. Only present when revised.</param>
/// <param name="Revisions">The revisions applied to this event, if any.</param>
/// <param name="GenerationalContent">Content for each generation stored for this event, keyed by generation number.</param>
public record AppendedEvent(
    EventContext Context,
    string Content,
    string OriginalContent = "",
    IEnumerable<EventRevision>? Revisions = null,
    IReadOnlyDictionary<int, string>? GenerationalContent = null);

