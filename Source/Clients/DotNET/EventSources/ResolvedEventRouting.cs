// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.EventSources;

/// <summary>
/// Represents the routing of an append that has been resolved from an <see cref="IEventSource"/> definition.
/// </summary>
/// <param name="Definition">The <see cref="EventSourceDefinition"/> the append goes through.</param>
/// <param name="Stream">The <see cref="EventStream"/> the append goes to, if any.</param>
internal record ResolvedEventRouting(EventSourceDefinition Definition, EventStream? Stream)
{
    /// <summary>
    /// Gets the <see cref="EventSourceName"/> to record on the event.
    /// </summary>
    public EventSourceName EventSource => new(Definition.Name);

    /// <summary>
    /// Gets the <see cref="EventSourceType"/> to write on the event.
    /// </summary>
    public EventSourceType SourceType => Definition.EventSourceType;

    /// <summary>
    /// Gets the <see cref="EventStreamType"/> to write on the event.
    /// </summary>
    public EventStreamType StreamType => Stream?.EventStreamType ?? EventStreamType.All;

    /// <summary>
    /// Gets the <see cref="ConcurrencyDimensions"/> that apply to the append.
    /// </summary>
    public ConcurrencyDimensions Dimensions => Definition.ConcurrencyFor(Stream);

    /// <summary>
    /// Resolve the routing of an append through an event source.
    /// </summary>
    /// <param name="eventSources">The <see cref="IEventSources"/>; null when the event store has none.</param>
    /// <param name="eventSource">The type of the <see cref="IEventSource"/>.</param>
    /// <param name="stream">The name of the stream, if any.</param>
    /// <param name="explicitEventSourceType">An explicit <see cref="EventSourceType"/>, if the caller gave one.</param>
    /// <param name="explicitEventStreamType">An explicit <see cref="EventStreamType"/>, if the caller gave one.</param>
    /// <returns>The <see cref="ResolvedEventRouting"/>.</returns>
    /// <exception cref="UnknownEventSource">The type is not a discovered event source.</exception>
    /// <exception cref="EventStreamDoesNotBelongToEventSource">The stream is not declared by the event source.</exception>
    /// <exception cref="EventRoutingContradictsEventSource">Explicit routing values contradict the definition.</exception>
    public static ResolvedEventRouting Resolve(
        IEventSources? eventSources,
        Type eventSource,
        string? stream,
        EventSourceType? explicitEventSourceType,
        EventStreamType? explicitEventStreamType)
    {
        var definition = eventSources?.GetFor(eventSource) ?? throw new UnknownEventSource(eventSource.FullName ?? eventSource.Name);

        if (explicitEventSourceType is not null &&
            explicitEventSourceType != EventSourceType.Default &&
            explicitEventSourceType != EventSourceType.Unspecified &&
            explicitEventSourceType != definition.EventSourceType)
        {
            throw new EventRoutingContradictsEventSource(definition.Name, nameof(EventSourceType), definition.Name, explicitEventSourceType.Value);
        }

        var explicitStream = explicitEventStreamType is not null && explicitEventStreamType != EventStreamType.All
            ? explicitEventStreamType.Value
            : null;

        if (stream is not null && explicitStream is not null && stream != explicitStream)
        {
            throw new EventRoutingContradictsEventSource(definition.Name, nameof(EventStreamType), stream, explicitStream);
        }

        var streamName = stream ?? explicitStream;
        if (streamName is null)
        {
            return new(definition, null);
        }

        var eventStream = definition.FindStream(streamName) ?? throw new EventStreamDoesNotBelongToEventSource(definition.Name, streamName);
        return new(definition, eventStream);
    }
}
