// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

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
}
