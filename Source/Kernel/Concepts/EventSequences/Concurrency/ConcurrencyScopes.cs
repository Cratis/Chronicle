// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.Concepts.EventSequences.Concurrency;

/// <summary>
/// Represents a concurrency scope for an event sequence append many operation.
/// </summary>
/// <param name="scopes">The scopes.</param>
public class ConcurrencyScopes(IDictionary<EventSourceId, ConcurrencyScope> scopes)
{
    /// <summary>
    /// Gets the scopes.
    /// </summary>
    public IDictionary<EventSourceId, ConcurrencyScope> Scopes { get; } = new Dictionary<EventSourceId, ConcurrencyScope>(scopes);

    /// <summary>
    /// Gets the <see cref="ConcurrencyScope"/> for the <see cref="EventSourceId"/>.
    /// If there is no <see cref="ConcurrencyScope"/> then <see cref="ConcurrencyScope.None"/> will be returned.
    /// </summary>
    /// <param name="eventSourceId">The <see cref="EventSourceId"/>.</param>
    /// <returns>The <see cref="ConcurrencyScope"/>.</returns>
    public ConcurrencyScope GetFor(EventSourceId eventSourceId) =>
        Scopes.TryGetValue(eventSourceId, out var scope) ? scope : ConcurrencyScope.None;
}
