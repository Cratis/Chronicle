// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.Storage.Seeding;

/// <summary>
/// Represents a single seeded event entry.
/// </summary>
/// <remarks>
/// Historic documents can assign null to missing routing fields during deserialization.
/// Both initialization and reads retain the defaults used by the original seeding API.
/// </remarks>
/// <param name="EventSourceId">The event source identifier.</param>
/// <param name="EventTypeId">The event type identifier.</param>
/// <param name="Content">The JSON content of the event.</param>
/// <param name="Tags">The collection of tags associated with the event.</param>
public record SeededEventEntry(EventSourceId EventSourceId, EventTypeId EventTypeId, string Content, IEnumerable<string>? Tags)
{
    /// <summary>
    /// Gets or inits the event source type.
    /// </summary>
    public EventSourceType EventSourceType
    {
        get => field ?? EventSourceType.Default;
        init => field = value ?? EventSourceType.Default;
    } = EventSourceType.Default;

    /// <summary>
    /// Gets or inits the event stream type.
    /// </summary>
    public EventStreamType EventStreamType
    {
        get => field ?? EventStreamType.All;
        init => field = value ?? EventStreamType.All;
    } = EventStreamType.All;

    /// <summary>
    /// Gets or inits the event stream identifier.
    /// </summary>
    public EventStreamId EventStreamId
    {
        get => field ?? new EventStreamId(EventStreamId.Default);
        init => field = value ?? new EventStreamId(EventStreamId.Default);
    } = EventStreamId.Default;
}
