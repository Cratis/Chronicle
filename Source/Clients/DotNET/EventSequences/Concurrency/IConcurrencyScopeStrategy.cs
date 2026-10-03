// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSources;

namespace Cratis.Chronicle.EventSequences.Concurrency;

/// <summary>
/// Defines a strategy for managing concurrency scopes.
/// </summary>
public interface IConcurrencyScopeStrategy
{
    /// <summary>
    /// Gets a <see cref="ConcurrencyScope"/> for the specified parameters.
    /// </summary>
    /// <param name="eventSourceId">The <see cref="EventSourceId"/> to scope to.</param>
    /// <param name="eventStreamType">Optional <see cref="EventStreamType"/> to scope to. Defaults to <see cref="EventStreamType.All"/>.</param>
    /// <param name="eventStreamId">Optional <see cref="EventStreamId"/> to scope to. Defaults to <see cref="EventStreamId.Default"/>.</param>
    /// <param name="eventSourceType">Optional <see cref="EventSourceType"/> to scope to. Defaults to <see cref="EventSourceType.Default"/>.</param>
    /// <param name="eventTypes">Optional collection of <see cref="EventType"/> to scope to. Defaults to no specific event types.</param>
    /// <returns>The <see cref="ConcurrencyScope"/>.</returns>
    Task<ConcurrencyScope> GetScope(
        EventSourceId eventSourceId,
        EventStreamType? eventStreamType = default,
        EventStreamId? eventStreamId = default,
        EventSourceType? eventSourceType = default,
        IEnumerable<EventType>? eventTypes = default);

    /// <summary>
    /// Gets the <see cref="ConcurrencyScope"/>, narrowed to the dimensions an event source or event stream declares.
    /// </summary>
    /// <param name="dimensions">The <see cref="ConcurrencyDimensions"/> that take part in the scope.</param>
    /// <param name="eventSourceId">The <see cref="EventSourceId"/> being appended to.</param>
    /// <param name="eventStreamType">Optional <see cref="EventStreamType"/> being appended to.</param>
    /// <param name="eventStreamId">Optional <see cref="EventStreamId"/> being appended to.</param>
    /// <param name="eventSourceType">Optional <see cref="EventSourceType"/> being appended to.</param>
    /// <param name="eventTypes">Optional event types to scope the check to.</param>
    /// <returns>The <see cref="ConcurrencyScope"/>.</returns>
    /// <remarks>
    /// <see cref="ConcurrencyDimensions.None"/> declares nothing and gives the same scope as the scope without dimensions.
    /// The default implementation leaves out the stream and event source type dimensions that are not declared, and
    /// always includes the event source id; a strategy that can also leave the event source id out overrides it.
    /// </remarks>
    Task<ConcurrencyScope> GetScope(
        ConcurrencyDimensions dimensions,
        EventSourceId eventSourceId,
        EventStreamType? eventStreamType = default,
        EventStreamId? eventStreamId = default,
        EventSourceType? eventSourceType = default,
        IEnumerable<EventType>? eventTypes = default) =>
        dimensions == ConcurrencyDimensions.None
            ? GetScope(eventSourceId, eventStreamType, eventStreamId, eventSourceType, eventTypes)
            : GetScope(
                eventSourceId,
                dimensions.HasFlag(ConcurrencyDimensions.EventStreamType) ? eventStreamType : null,
                dimensions.HasFlag(ConcurrencyDimensions.EventStreamId) ? eventStreamId : null,
                dimensions.HasFlag(ConcurrencyDimensions.EventSourceType) ? eventSourceType : null,
                eventTypes);
}
