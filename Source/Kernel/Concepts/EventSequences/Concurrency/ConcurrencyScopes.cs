// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.Concepts.EventSequences.Concurrency;

/// <summary>
/// Represents a concurrency scope for an event sequence append many operation.
/// </summary>
public class ConcurrencyScopes : Dictionary<EventSourceId, ConcurrencyScope>
{
    /// <summary>
    /// Gets the <see cref="ConcurrencyScope"/> for the <see cref="EventSourceId"/>.
    /// If there is no <see cref="ConcurrencyScope"/> then <see cref="ConcurrencyScope.None"/> will be returned.
    /// </summary>
    /// <param name="eventSourceId">The <see cref="EventSourceId"/>.</param>
    /// <returns>The <see cref="ConcurrencyScope"/>.</returns>
    public ConcurrencyScope GetFor(EventSourceId eventSourceId) =>
        TryGetValue(eventSourceId, out var scope) ? scope : ConcurrencyScope.None;
}