// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.Storage.EventSources;
using Cratis.Monads;

namespace Cratis.Chronicle.EventSources;

/// <summary>
/// Validates an append against the registered event source definitions and resolves the event source type to write.
/// </summary>
internal static class EventSourceResolution
{
    /// <summary>
    /// The error code reported when an append refers to an event source that is not registered.
    /// </summary>
    internal const string UnknownEventSource = nameof(UnknownEventSource);

    /// <summary>
    /// The error code reported when an append refers to an event stream that does not belong to the event source.
    /// </summary>
    internal const string UnknownEventStreamForEventSource = nameof(UnknownEventStreamForEventSource);

    /// <summary>
    /// The error code reported when the event source type of an append contradicts the event source it is appended through.
    /// </summary>
    internal const string EventSourceTypeDoesNotMatchEventSource = nameof(EventSourceTypeDoesNotMatchEventSource);

    /// <summary>
    /// Resolves the event source type for an append, validating the event source and event stream against the registered definition.
    /// </summary>
    /// <param name="eventSources">The <see cref="IEventSourcesStorage"/> holding the registered definitions.</param>
    /// <param name="eventSource">The name of the event source the event is appended through, if any.</param>
    /// <param name="eventSourceType">The requested <see cref="EventSourceType"/>.</param>
    /// <param name="eventStreamType">The requested <see cref="EventStreamType"/>.</param>
    /// <param name="correlationId">The <see cref="CorrelationId"/> of the append, used when reporting failure.</param>
    /// <returns>The event source type to write, or a failed <see cref="AppendResult"/>.</returns>
    /// <remarks>
    /// An append that does not name an event source is accepted exactly as before, even when its event source type
    /// happens to match a registered definition.
    /// </remarks>
    internal static async Task<Result<EventSourceType, AppendResult>> Resolve(
        IEventSourcesStorage eventSources,
        EventSourceName? eventSource,
        EventSourceType eventSourceType,
        EventStreamType eventStreamType,
        CorrelationId correlationId)
    {
        if (eventSource?.IsSet != true)
        {
            return eventSourceType;
        }

        var definition = await eventSources.Find(eventSource);
        if (definition is null)
        {
            return Failed(correlationId, UnknownEventSource, $"The event source '{eventSource}' is not registered.");
        }

        if (!eventSourceType.IsDefaultOrUnspecified && eventSourceType != definition.EventSourceType)
        {
            return Failed(
                correlationId,
                EventSourceTypeDoesNotMatchEventSource,
                $"The event source type '{eventSourceType}' does not match the event source '{eventSource}'.");
        }

        if (!eventStreamType.IsAll && definition.GetStream(eventStreamType) is null)
        {
            return Failed(
                correlationId,
                UnknownEventStreamForEventSource,
                $"The event stream '{eventStreamType}' does not belong to the event source '{eventSource}'.");
        }

        return definition.EventSourceType;
    }

    static AppendResult Failed(CorrelationId correlationId, string code, string message) =>
        AppendResult.Failed(correlationId, [new AppendError($"{code}: {message}")]);
}
