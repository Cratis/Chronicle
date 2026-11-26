// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;
using Cratis.Chronicle.Concepts.Auditing;
using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.EventSequences;

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
/// <param name="Occurred">Optional occurred time. If null, the server will set it to approximately the time of append.</param>
/// <param name="Subject">Optional subject that identifies the compliance target for the event.</param>
[GenerateSerializer]
[Alias("Cratis.Chronicle.EventSequences.EventToAppend")]
public record EventToAppend(
    [Id(0)] EventSourceType EventSourceType,
    [Id(1)] EventSourceId EventSourceId,
    [Id(2)] EventStreamType eventStreamType,
    [Id(3)] EventStreamId eventStreamId,
    [Id(4)] EventType EventType,
    [Id(5)] IEnumerable<Tag> Tags,
    [Id(6)] JsonObject Content,
    [Id(7)] DateTimeOffset? Occurred = null,
    [Id(8)] Subject? Subject = null)
{
    /// <summary>
    /// Gets the structured named tags for this event.
    /// </summary>
    [Id(9)]
    public IReadOnlyCollection<NamedTag> NamedTags { get; init; } = [];

    /// <summary>
    /// Gets the name of the registered event source definition the event is appended through, if any.
    /// </summary>
    [Id(10)]
    public EventSourceName? EventSource { get; init; }

    /// <summary>
    /// Gets or inits the optional event-specific causation chain. When absent, the batch chain is used.
    /// </summary>
    [Id(11)]
    public IEnumerable<Causation>? Causation { get; init; }
}
