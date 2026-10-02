// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.EventSources;

/// <summary>
/// Represents a discovered <see cref="IEventSource"/> definition.
/// </summary>
/// <param name="ClrType">The type carrying the definition.</param>
/// <param name="Name">The name of the event source, which is the event source type of appended events.</param>
/// <param name="Description">The description of the event source.</param>
/// <param name="Concurrency">The default dimensions that take part in concurrency checks.</param>
/// <param name="Streams">The streams of the event source.</param>
public record EventSourceDefinition(
    Type ClrType,
    string Name,
    string Description,
    ConcurrencyDimensions Concurrency,
    IReadOnlyList<EventStream> Streams)
{
    /// <summary>
    /// Gets the name as an <see cref="Events.EventSourceType"/>.
    /// </summary>
    public Events.EventSourceType EventSourceType => new(Name);

    /// <summary>
    /// Find a stream by name.
    /// </summary>
    /// <param name="name">The name of the stream.</param>
    /// <returns>The <see cref="EventStream"/> if the definition has it; otherwise null.</returns>
    public EventStream? FindStream(string name) => Streams.FirstOrDefault(_ => _.Name == name);

    /// <summary>
    /// Gets the dimensions that apply to an append, from the stream when one is given and declares any, otherwise from the event source.
    /// </summary>
    /// <param name="stream">The stream the append is to, if any.</param>
    /// <returns>The effective <see cref="ConcurrencyDimensions"/>.</returns>
    public ConcurrencyDimensions ConcurrencyFor(EventStream? stream) =>
        stream is not null && stream.Concurrency != ConcurrencyDimensions.None ? stream.Concurrency : Concurrency;
}
