// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.Concepts.EventSources;

/// <summary>
/// Represents the definition of an event source, what it is and which event streams it has.
/// </summary>
/// <param name="Name">The <see cref="EventSourceName"/>, which becomes the <see cref="EventSourceType"/> of appended events.</param>
/// <param name="Description">The description of the event source.</param>
/// <param name="Owner">Who owns the definition.</param>
/// <param name="Concurrency">The default <see cref="ConcurrencyDimensions"/> for the event source.</param>
/// <param name="Streams">The streams of the event source.</param>
public record EventSourceDefinition(
    EventSourceName Name,
    EventSourceDescription Description,
    EventSourceOwner Owner,
    ConcurrencyDimensions Concurrency,
    IEnumerable<EventStreamDefinition> Streams)
{
    /// <summary>
    /// Gets the <see cref="EventSourceType"/> written on events appended through this definition.
    /// </summary>
    public EventSourceType EventSourceType => new(Name.Value);

    /// <summary>
    /// Gets the stream with the given name, if it belongs to this event source.
    /// </summary>
    /// <param name="name">The name of the stream.</param>
    /// <returns>The <see cref="EventStreamDefinition"/> if found; otherwise null.</returns>
    public EventStreamDefinition? GetStream(EventStreamType name) => Streams.FirstOrDefault(_ => _.Name == name);
}
